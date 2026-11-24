# Fleet and GPS review

Reviewed on 2026-11-24 for schedule row 9 and US-18. Backend baseline: `d47cbb1`; frontend baseline: `0936957`. This records the existing behavior and the corrections planned for row 11.

## Verified behavior

An isolated local MySQL database was used. Twenty-nine principal HTTP checks covered registration and retrieval, normalized duplicate plates, warehouse permissions, missing authentication, resident rejection, GPS normalization and repeat pairing, occupied-device rejection, pairing filters, and position updates to the linked vehicle. The occupied-device response identifies the current vehicle in its message.

Five diagnostic input requests exposed defects. A separate seven-request concurrent-assignment scenario returned one `200` and one `409`; exactly one vehicle retained the GPS. The total was 41 HTTP requests, including diagnostics, rather than 41 passing tests. No Azure database or GPS hardware was accessed.

## Corrections for row 11

| ID | Finding | Evidence |
| --- | --- | --- |
| F9-01 | A 101-character brand or 65-character GPS identifier produces `500`; validate lengths before persistence and return `400`. | HTTP and EF length configuration |
| F9-02 | Capacity `0.001` is accepted and persisted as zero after rounding; validate the stored precision. | HTTP and `VehicleCapacity` |
| F9-03 | A punctuation-only plate is accepted although the validation message promises alphanumeric input. | HTTP and `LicensePlate` |
| F9-04 | The API returns `FLEET_DEVICE_ALREADY_ASSIGNED`, with the owner plate only in text. The frontend expects a different code and `assignedLicensePlate`. Align the structured conflict contract. | HTTP and both GPS forms |
| F9-05 | The pairing page depends on cached vehicles. Fresh detail, loading/error states, submission/navigation guards and bounded requests need review in the frontend. | Code inspection; no browser execution in row 9 |
| F9-06 | Replacing a GPS retains the previous device's position. Coordinate replacement semantics with the telemetry review in row 10. | HTTP and `Vehicle.PairGpsDevice` |

US-18 remains partially complete until the corrections are implemented and verified. Geofences and telemetry are reviewed in row 10; departure and dispatch-linked tracking belong to row 12. The review does not introduce an active/disabled vehicle state. Local MySQL 9.6 verification does not replace production acceptance on Azure MySQL 8.4.
