# vigia-platform

## Database migrations

Requires .NET 10 and MySQL. Store `ConnectionStrings:DefaultConnection` in user-secrets for `VigiaPlatform.API`; do not commit database credentials.

For the first migration, use an empty database. Existing databases created with `EnsureCreated()` must be preserved or backed up and transitioned separately; do not apply `InitialCreate` over their existing tables.

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
