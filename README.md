# vigia-platform

## Azure deployment

Deployment now targets an Ubuntu VM in Azure, with the API and MySQL managed by Docker Compose and Caddy providing HTTPS. The frontend remains on Firebase Hosting. Follow [the Azure resource and deployment guide](deploy/azure/README.md).

The API supports one-off `--migrate` and `--bootstrap-admin` operations, persistent Data Protection keys through `DataProtection__KeysPath`, forwarded headers from `ReverseProxy__KnownProxy`, opt-in production Scalar through `OpenApi__Enabled=true`, and a database connectivity endpoint at `/health`. Production startup does not run the local demo setup. Private development settings are excluded from publish output.

## Database migrations

Requires .NET 10 and MySQL. Store `ConnectionStrings:DefaultConnection` in user-secrets for `VigiaPlatform.API`; do not commit database credentials.

For the first migration, use an empty database. Existing databases created with `EnsureCreated()` must be preserved or backed up and transitioned separately; do not apply `InitialCreate` over their existing tables.

Both Development startup and the `--migrate` operation reject a nonempty database that does not have this migration chain's initial history entry, before changing its schema. In Rider, select a profile connected to the migrated database. The normal Run button does not apply the temporary connection override performed by `scripts/start-local.ps1`.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project VigiaPlatform.API
```

Development startup applies pending migrations. Demo data is opt-in and only allowed in a database prefixed `vigia_local_`. Production startup does not apply migrations or seed sample data. Apply migrations separately during deployment, before starting the new API version. Generate a SQL script for review with:

```powershell
dotnet ef migrations script --project VigiaPlatform.API --output migration.sql
```

Commit each migration with its designer file and `AppDbContextModelSnapshot`. Coordinate schema changes through one migration branch at a time, based on the current `develop` model. Do not use `EnsureCreated()` with migrations.

## Local frontend integration

With MySQL running, execute from this repository:

```powershell
.\scripts\start-local.ps1
```

The script reads the private `VigiaPlatform.API/appsettings.Development.json`, changes only the database name in the process configuration to `vigia_local_integration`, applies the migration chain, and seeds linked demo records. It preserves the database named in your original file. The MySQL user needs permission to create the separate local database. Use `-ConfigurationFile` for a different private JSON file, `-Database vigia_local_other` for a separate local database, or `-Dotnet` for an explicit .NET 10 executable.

API: `http://localhost:5125/api/v1`. Scalar: `http://localhost:5125/scalar`.

Local accounts are `admin.local`, `warehouse.local`, and `site.local`. Their generated password is stored in `.local/access.json`, excluded from Git. Keep that file to retain the credentials for an already seeded database. Self-registration creates a site-manager account; it cannot grant administrative permissions. The administrator can use all three frontend perspectives without changing identity. Regular accounts retain their assigned role.

From `Vigia-Frontend`, run `npm run dev` and open `http://localhost:5173`. Its development proxy forwards `/api/v1` to this API; the in-memory API is disabled in development. The existing Firebase deployment and its production configuration are separate from this local setup.

Sign-in returns an ASP.NET Core opaque bearer token lasting eight hours. The frontend sends it on API requests and clears its session on sign-out or an unauthorized response. It stores passwords only as server-side hashes. Configure a persistent Data Protection key ring before deploying multiple or ephemeral backend instances.

Projects, materials, inventory movements and activity history are persisted queries. Dispatch creation derives expected materials from an approved order and a registered project. Status transitions use domain commands. Physical reception compares received quantities with persisted dispatch lines and records the authenticated resident; it creates a discrepancy case transactionally when needed. The original weighed-receipt API still requires actual scale measurements and a graphic signature. Physical verification does not fabricate either. Evidence endpoints store URL references; uploading binary files to cloud storage remains a deployment task. Supporting reports download real PDFs containing the saved case, materials, evidence references and timeline.

## Integration verification

Run the checks against an isolated local database, never the original database:

```powershell
.\scripts\start-local.ps1 -Database vigia_local_validation
```

In a second terminal:

```powershell
python .\scripts\verify-local.py --allow-local-writes
dotnet run --project .\tests\VigiaPlatform.IntegrationChecks -- .\VigiaPlatform.API\appsettings.Development.json
```

The HTTP checks exercise authentication, role restrictions, profile ownership, persisted queries, order transitions, telemetry, reception updates/comparison, conformity, discrepancy handling, case closure, history and PDF export. They create test records. The .NET checks create a separate timestamped `vigia_local_atomicity_...` database and force an activity-write failure to verify rollback, then check successful atomic persistence and child-only telemetry history. Test databases are retained for inspection.

## Resident material requests (US-03)

`GET /api/v1/orders/request-catalog` returns assigned projects and requestable material IDs/names/units to `SITE_MANAGER`; it does not expose warehouse stock. `ADMIN` can request the full catalog. Projects must be assigned by the authenticated account ID in `ManagerId`; changing a display name does not grant access. Materials with no project association are shared catalog entries; project-specific materials are restricted to that project.

`POST /api/v1/orders/requests` accepts `projectId`, `priority` (`LOW`, `AVERAGE`, `HIGH`), `date` (`yyyy-MM-dd`), `observations` and `items` containing `materialId`, positive integer `quantity` and optional `observation`. The server resolves project/material names and units, derives the requester from authentication and generates the `SOL-...` code. It returns `201` with the existing order resource in `PENDING`. Invalid lines return `400`, an unassigned project or role returns `403`, and a generated-code conflict returns `409`. Submission makes the request available in the warehouse pending queue; it does not automatically approve it.

Residents can query `/orders` (including existing status/code filters) and `/orders/{id}` only for their own account. Other residents' IDs and historical unowned orders return `404`. Warehouse/admin list and review permissions remain; legacy creation, status changes and attached-file mutation are not opened to residents. No ownership is inferred from `requesterName`.

Apply the additive `AddOrderRequesterAccount` migration through the existing controlled deployment sequence. Existing rows retain `NULL` ownership and remain available to warehouse/admin. Review a separate controlled backfill if historical resident access is required. Do not rewrite `InitialCreate` or run migrations against an old EnsureCreated database. Production catalog loading remains a separate reviewed operation; do not enable the local demo seeder there. Scalar documents the new routes without replacing its existing setup.

For local verification, start a fresh database with `scripts/start-local.ps1 -Database vigia_local_resident_check -Port 5135`, then run:

```powershell
python scripts/verify-resident-orders.py --allow-local-writes
```

The checks exercise assigned catalog access, spoofed identity fields, validation, isolation between accounts with equal display names, warehouse review and historical-order access. They create local-only fixtures and never contact Azure.

## Dispatch net weighing (US-04 / TS-02)

`POST /api/v1/dispatches/{dispatchId}/weighing` records net cargo weight in kg for an `IN_PREPARATION` dispatch with an assigned vehicle. `ADMIN` and `WAREHOUSE_MANAGER` can record and query measurements; residents cannot use these routes. The API returns `201` and a resource `Location`, `404` for an unknown dispatch, `400` for invalid values and `409` for an incompatible state, conflicting retry or concurrent modification. Scalar keeps these operations under **Dispatches**.

```json
{
  "weightKg": 1000.125,
  "expectedWeightKg": 1000,
  "toleranceKg": 2,
  "requestId": "17877d26-32e2-40bc-98e8-e7cd5cdbb6cf"
}
```

The warehouse supplies the expected net weight and absolute tolerance in kg. The server compares `abs(weightKg - expectedWeightKg) <= toleranceKg`, including the boundary. It never infers kg from piece/unit counts, subtracts a vehicle's tare implicitly or invents a default tolerance. Measurements support up to three decimals and the storage range of `decimal(12,3)`; weights must be positive and tolerance may be zero. Supply both comparison fields or neither. A weight-only request is saved as `NOT_EVALUATED`, with review required, rather than classified as acceptable without evidence.

`GET /api/v1/dispatches/{dispatchId}/weighing` returns the measurement history; appending `/{weighingId}` retrieves one record belonging to that dispatch. Records retain the authenticated operator, UTC time and vehicle/load snapshot. There is no edit/delete route for weighing history. An optional UUID `requestId` makes identical retries return the original resource without duplicate measurement, timeline or audit records. Reusing it with different values returns `409`.

Dispatch resources now include `latestWeighing` and `requiresWeightReview`. Vehicle or material-line changes invalidate the old measurement for the current load while preserving its original comparison and history. A new valid weighing clears that weight review. Aggregate version checking rejects stale writes, and the weighing, dispatch timeline and activity audit are committed together. A review flag does not introduce another dispatch status. Guide issuance and departure require the current weighing to be within tolerance.

Apply `AddDispatchNetWeighings` after `AddOrderRequesterAccount`, preserving the existing migration chain. Historical dispatches receive version zero and no fabricated measurements. The frontend provides weighing and guide issuance. Azure publication remains a separate release task.

For isolated verification, start `scripts/start-local.ps1 -Database vigia_local_weighing_check -Port 5145`, then run `python scripts/verify-dispatch-weighing.py --allow-local-writes`. The HTTP checks cover precision, both sides of the tolerance boundary, missing comparison data, authentication, audit, load/vehicle changes, replay and simultaneous retries. The .NET integration checks also force a weighing audit failure and a stale concurrent write to verify transactional rollback.

`POST /api/v1/dispatches/{dispatchId}/delivery-guide` accepts `driverName`, `carrierName` and an optional UUID `requestId`. Warehouse/admin can issue one guide only for an assigned vehicle with a current, conforming weighing. `GET` on the same path retrieves its immutable cargo, vehicle, weighing and operator snapshot. Identical retries reuse the original guide; conflicting requests return `409`. Scalar documents both operations under Dispatches.

Apply `AddDispatchDeliveryGuides` after `AddDispatchNetWeighings`. Existing dispatches receive no fabricated guides. Issuing a guide authorizes departure and locks load, vehicle and new weighing changes. Moving to `IN_TRANSIT` requires this authorization; cancellation preserves the guide and revokes authorization. Upcoming arrivals expose the guide number and driver for the resident's later receipt workflow. Receipt verification remains a separate integration task.

Run `python scripts/verify-dispatch-guides.py --allow-local-writes` against an isolated local API. The checks cover authorization, snapshots, exact replay, concurrent issuance, cancellation and departure, alongside the existing weighing and order regressions.

Fleet registration validates normalized plates, brand/model and GPS lengths, and positive capacity at the stored precision. Occupied GPS responses include `assignedLicensePlate`; assigning the same device again retains its position, while replacing or unlinking it clears the previous device position. Position requests require both coordinates, including when either value is legitimately zero.

Geofences use a circular center and a radius of 10 to 50,000 meters. Job-site IDs reference existing projects. A company/site can register one new geofence; concurrent duplicates return `409`. Apply `AddGeofenceSiteRegistrations` after `AddDispatchDeliveryGuides`. It reserves existing company/site keys against their first geofence without deleting duplicate historical records. New geofence and site registration are committed atomically. Run `python scripts/verify-fleet-geofences.py --allow-local-writes` against the isolated local API. Device timestamp ordering and active dispatch/route association are implemented by the departure flow below.

## Dispatch departure and GPS tracking (US-06 / TS-08)

Warehouse/admin starts the actual trip with `POST /api/v1/dispatches/{id}/departure`. `GET` on the same path supplies warehouse geofences, the destination project geofence, paired GPS and any existing trip. The body contains `originGeofenceId`, a future ISO-8601 `estimatedArrival`, `plannedPath` with 2–500 `{latitude,longitude}` points, and a non-empty UUID `requestId`. The path starts inside the chosen warehouse circle and ends inside the dispatch project circle. Actor, actual departure time, vehicle and destination are determined by the server.

Current conforming weighing, an issued guide, the assigned vehicle and a paired GPS are required. Only one active trip or unfinished in-transit dispatch may reserve that vehicle. Route, GPS binding, dispatch state/timeline and activity are saved atomically. Exact UUID/payload retries return the original route (`201`), including after arrival or closure; a different request conflicts. `PATCH /dispatches/{id}/status` cannot start transit, and legacy `POST /routes` returns `409 START_TRIP_REQUIRED`. Clients must use the departure operation instead. Existing route queries and historical records remain available.

`POST /api/v1/telemetry/reports` accepts `gpsDeviceId`, required `latitude` and `longitude`, optional ISO-8601 `timestamp` and optional `speedKmh` (0–300). Timestamps default to server time, cannot predate 1970 or exceed server time by five minutes, and retain UTC microsecond precision. Older reports or conflicting payloads at the same timestamp return `409`; exact replays return `200` without another route position. Vehicle position, current trip history, alerts and activity commit together. Optimistic concurrency prevents stale reports, device replacement and trip closure from silently overwriting each other.

An unknown GPS is retained as an unassociated event (`accepted:false`, `recorded:true`). A paired GPS without an active trip updates only the ordered vehicle position (`tracked:false`, `reason:NO_ACTIVE_TRIP`). GPS devices from another company are rejected. GPS replacement and manual vehicle-position edits are blocked while the trip is active. Arrival releases route tracking and raises the geofence-entry event; it does not certify receipt or automatically complete the dispatch. A vehicle with an unfinished in-transit dispatch remains unavailable for another departure.

Upcoming arrivals includes the real route ID, origin, departure time and estimated arrival. Residents can read only routes/arrivals for their assigned projects. The destination notice is persisted in-app information refreshed by the frontend; email, push and physical GPS-device acceptance are outside this implementation. Telemetry ingestion continues to require the authenticated warehouse/admin bearer token. Route resources identify `trackingSource` (`DEVICE` or historical `LEGACY`), `trackingActive`, `gpsDeviceId` and `departureRequestId`.

Apply `AddDispatchTripsAndPositionOrdering` after `AddGeofenceSiteRegistrations`. Existing positions retain no invented timestamp; historical routes acquire no fictitious dispatch/GPS binding. Verification: `python scripts/verify-dispatch-transit.py --url http://localhost:5169/api/v1 --allow-local-writes` against an isolated demo database, plus existing HTTP and .NET integration checks. Scalar remains the API documentation. Azure migration/publication belongs to the final release session.

Row 13 contract guard: order-to-dispatch planning must resolve the order's project name snapshot to exactly one existing project. Missing or duplicate names return a validation error without creating a dispatch, rather than selecting an arbitrary site or returning an unhandled query exception. The current order model does not persist a canonical project ID; retaining it is a separate model change. Existing unique-name flows and numeric dispatch/vehicle identifiers remain compatible. Native integration checks cover the ambiguous-name transaction, and frontend integration-contract tests retain weighing/guide IDs and distinguish route arrival from dispatch completion.

Row 13 integrated review: `python scripts/verify-integration-contracts.py --url http://localhost:5171/api/v1 --allow-local-writes` exercises the real resident request, warehouse approval, immutable weighing/guide, device-bound departure, GPS arrival and existing reception/case/PDF boundary against an isolated local API. Its JSON report distinguishes verified foundations from open external-contract findings; a completed review does not imply accepted reception or a safe final release. See `docs/reviews/integration-contracts-review.json` for reproducible observations and the A01/A02/A03 handoff. Andre has not yet delivered A01; final reception closure, project authorization, guide integrity and persisted evidence remain explicit acceptance gates.

Discrepancy boundary reviewed in row13: case resources link textual `dispatchId` and `receptionId` to numeric resource IDs. Closing requires a determined liability; the current API permits an omitted resolution summary. Conflict responses may be plain text, so contract verification does not assume every error is JSON. The local report checks case linkage, timeline, closure and PDF response; it records unassigned-resident PDF access and URL-only evidence as open findings for rows15/16 and Andre's handoff. A reference URL is not proof of an uploaded file.

Stored photographs and reception integration: [API contract and verification](docs/reviews/stored-photographs-contract.md).
