# Geofence and telemetry review

Reviewed on 2026-11-24 for schedule row 10, US-19 and TS-08. Baseline after integrating row 9: backend `687f2db`, frontend `7e434e2`. The accepted geofence is a circle with configurable center and radius that covers the entire property; polygon drawing is outside the agreed scope.

## Executed verification

An isolated local database received 39 HTTP requests: 34 expected-status checks and five diagnostic probes. Checks covered warehouse/resident permissions, required authentication, circular geofence creation and retrieval, warehouse/job-site filters, invalid coordinates/types, missing fields, radius limits of 10 and 50,000 meters, paired-device normalization and position updates, and unpaired telemetry.

An unknown device returned `accepted: false`, `recorded: true`, `reason: UNPAIRED_DEVICE`. A direct local MySQL query confirmed exactly one event with its reported coordinates. The paired vehicle's position remained unchanged by that event.

Eight checks against the actual C# domain source covered center, inside/outside positions, zero distance, radius precision, valid radius limits and invalid latitude. Five frontend domain checks verified center/inside/outside/null containment and zero distance. No new browser, hardware GPS or Azure validation is claimed.

## Findings and follow-up

| ID | Finding | Follow-up |
| --- | --- | --- |
| F10-01 | The UI offers five hardcoded demonstration properties instead of an authoritative site catalog. | Row 11: real project/site selection and explicit warehouse configuration; do not invent production data. |
| F10-02 | The API accepts multiple geofences for the same company/site (`201`); the UI expects `SITE_ALREADY_HAS_GEOFENCE`/`409` and the domain describes one fence per property. | Row 11: reconcile the policy and existing records before any constraint migration. |
| F10-03 | UI radius validation stops at 5,000 meters; the API accepts up to 50,000. A 65-character site or 151-character name returns `500`. | Row 11: align limits and validate lengths before persistence. |
| F10-04 | An omitted telemetry latitude becomes zero and returns `200`. | Row 11/12: distinguish absent coordinates from valid zero coordinates. |
| F10-05 | An old paired-device report returns `200` and overwrites the newer position. The paired path ignores the supplied timestamp. | Row 12: preserve ordering and timestamp evidence rather than moving the vehicle backwards in time. Coordinate GPS replacement from F9-06. |
| F10-06 | `/telemetry/reports` updates a vehicle or retains an unknown-device event, but does not write a tracked route position or bind the report to an active dispatch. The tracking UI uses a separate simulator/route update path. | Row 12: integrate device, vehicle, active authorized dispatch and route. Do not count simulated tracking as physical device acceptance. |
| F10-07 | The geofence domain accepts a `NaN` radius when called directly. Ordinary JSON cannot submit this literal. | Row 11: enforce finite domain values for internal callers as well as HTTP validation. |

The circle model is already implemented. Completing this review does not complete the corrections or US-19/TS-08 acceptance. Keep product tracking partial until the affected rows are implemented and tested. Production backup, release and Azure verification remain in the release schedule.
