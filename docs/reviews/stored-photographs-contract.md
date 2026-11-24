# Stored photographs and discrepancy cases

Elías, row 15. Contract for André A03; final integrated acceptance remains row 16 after A01–A03 are delivered. Scalar remains the API reference.

Upload the actual file **before** completing physical verification. A completed reception cannot receive a new photograph. Cases use the reception/dispatch IDs returned by the server; do not create a second case after `POST /receptions/{id}/verify` has already created one.

| Operation | Authenticated endpoint | Result |
| --- | --- | --- |
| Read permitted receipt/evidence context | `GET /api/v1/receptions/{id}/evidence-context` | Existing reception resource, including evidence metadata |
| Upload | `POST /api/v1/receptions/{id}/evidence/files` | Multipart fields `file`, `description`, `requestId` (UUID);201 with photo metadata and relative `fileUrl` |
| Retrieve photo | `GET /api/v1/receptions/{id}/evidence/files/{fileId}` | Original bytes; attachment response, private/no-store, nosniff |
| Read receipt linked to permitted case | `GET /api/v1/discrepancy-cases/{id}/reception` | Canonical receipt context;404 if unavailable or unauthorized |

Use a Bearer token for upload, context, case, timeline, PDF and download. The resident must be assigned to the actual project; administrator access is explicit.404 hides unknown/unpermitted resources; warehouse role403. Filtering in the browser does not grant access.

JPEG/PNG only: matching media type and extension, signature/header and dimension checks, at most5MiB/16MP per photo, at most20 stored photos per reception, caption1–250 characters, filename1–120 characters. Request body limit6MiB. No full decoder or malware-scanner acceptance is claimed. Files and their metadata are stored together in the existing MySQL database; include this table in the scheduled database backup.

Keep the same UUID, file bytes, name, media type and caption after a timeout or lost response. An exact replay returns the original file, including after receipt closure; a different payload using that UUID returns409. The browser retains pending identity/SHA256 in sessionStorage, scoped to account and receipt; after reload reselect the same local file. File bytes are not stored in browser storage. Photo, receipt evidence and audit commit together; rollback leaves no orphan photo.

The old JSON `/receptions/{id}/evidence` endpoint records an **external HTTP(S) reference**, not a stored file. It cannot mint a stored-file route. The UI no longer uses example photographs or arbitrary external URLs as proof of upload, and only fetches protected images through the canonical relative file route. A URL alone does not demonstrate uploaded bytes.

The report page first uploads and confirms a photograph, then calls the existing physical-verification endpoint with current checklist values and observations. On reporting failure it refreshes receipt state; saved photographs are retained. Images and downloads in case/reception views use authenticated blob URLs, revoked when no longer needed. Legacy administrator cases without a canonical receipt remain readable as historical case records, with no invented photo.

Verified:106 evidence HTTP checks,82 existing-flow HTTP checks on the final run (85 on an earlier fixture state),19 frontend tests, frontend build, native MySQL audit rollback/retry and migration12→13 preserving29 existing tables. Browser: upload, report, ES/EN, photo retained after API restart; downloaded SHA256 matched original PNG. Exact/concurrent replay tested through HTTP; no browser network-fault injection claimed. NuGet vulnerability-feed lookup was unavailable; compilation and native tests passed. Existing Vite chunk-size warning remains.

F13-04 is resolved in this scope. F13-05 now has a verified upload/download implementation; final receipt integration still needs André A03. F13-02/03/06/07 remain André's receipt/guide/dispatch-closure work. Historical row13 diagnostic findings are preserved; this document records the newer evidence result. No release or cloud deployment is implied.


Row16 update (2026-11-24): André A01–A03 received and integrated. Backend4ce0e05/97c6493; frontend15d9bce/dada9a7. Local acceptance:84 strict integration HTTP checks,106 photo checks,82 existing-flow regression,25 frontend tests/build, native MySQL transaction/ownership/active-trip-release checks and migration13→14 preserving30 existing tables. PDF assertions include the case number and actual stored photo ID/caption. F13-02/03/04/05/06/07 are resolved for this tested local integration. Historical pending statements above describe the row15 baseline. Production publication and hardware acceptance are separate. See reception-evidence-integration.json.
