#!/usr/bin/env python3
"""Verify A04 (TS-06 / T-15) POST/GET /api/v1/contact-requests against a running local API."""
import argparse
import json
import sys
import urllib.error
import urllib.request
import uuid


def http_json(method, url, payload=None, token=None, origin=None):
    data = json.dumps(payload).encode("utf-8") if payload is not None else None
    headers = {"Accept": "application/json"}
    if data is not None:
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    if origin:
        headers["Origin"] = origin
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            body = resp.read().decode("utf-8")
            return resp.status, dict(resp.headers.items()), json.loads(body) if body else None
    except urllib.error.HTTPError as err:
        body = err.read().decode("utf-8")
        try:
            parsed = json.loads(body) if body else None
        except json.JSONDecodeError:
            parsed = body
        return err.code, dict(err.headers.items()), parsed


def sign_in(base_url, email, password):
    status, _, body = http_json("POST", f"{base_url}/authentication/sign-in", {"email": email, "password": password})
    assert status == 200 and body and body.get("accessToken"), f"Sign-in failed for {email}: {status} {body}"
    return body["accessToken"]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--url", default="http://localhost:5184/api/v1")
    parser.add_argument("--admin-email", default="admin@vigia.local")
    parser.add_argument("--resident-email", default="site@vigia.local")
    parser.add_argument("--password", required=True)
    args = parser.parse_args()

    base = args.url.rstrip("/")
    origin = "https://upc-pre-202620-1asi0730-8084-developers.github.io"
    checks = 0

    # 1. Missing required fields -> 400 listing missing fields
    st, hdrs, body = http_json("POST", f"{base}/contact-requests", {
        "fullName": "Ana",
        "email": "not-an-email"
    }, origin=origin)
    assert st == 400, f"Expected 400 for invalid payload, got {st}: {body}"
    assert body and body.get("code") == "VALIDATION_ERROR", f"Expected VALIDATION_ERROR: {body}"
    errors = body.get("errors", {})
    for field in ("companyName", "email", "segment", "message"):
        assert field in errors, f"Missing expected field error '{field}': {errors}"
    checks += 1

    # 2. Valid public submission -> 201 Created + CORS header for GitHub Pages
    req_id = str(uuid.uuid4())
    valid_payload = {
        "requestId": req_id,
        "fullName": "Carlos Mendoza",
        "companyName": "Constructora Andina SAC",
        "email": "cmendoza@andina.pe",
        "phone": "+51 987 654 321",
        "segment": "CONSTRUCTION",
        "plan": "PRO",
        "message": "Queremos implementar trazabilidad de materiales en tres frentes de obra.",
        "locale": "es"
    }
    st, hdrs, created = http_json("POST", f"{base}/contact-requests", valid_payload, origin=origin)
    assert st == 201, f"Expected 201 Created, got {st}: {created}"
    assert created["id"] > 0 and created["status"] == "RECEIVED", f"Unexpected body: {created}"
    cors_val = hdrs.get("Access-Control-Allow-Origin") or hdrs.get("access-control-allow-origin")
    assert cors_val == origin, f"Expected CORS header {origin}, got {cors_val}"
    checks += 1

    # 3. Exact idempotent retry -> 201 with same ID (no duplicate)
    st, _, replayed = http_json("POST", f"{base}/contact-requests", valid_payload, origin=origin)
    assert st == 201 and replayed["id"] == created["id"], f"Idempotent retry failed: {st} {replayed}"
    checks += 1

    # 4. Same requestId with different payload -> 409 Conflict
    st, _, conflict = http_json("POST", f"{base}/contact-requests", {**valid_payload, "companyName": "Otra SAC"})
    assert st == 409 and conflict.get("code") == "REQUEST_ID_CONFLICT", f"Expected 409 conflict: {st} {conflict}"
    checks += 1

    # 5. Anonymous GET -> 401
    st, _, _ = http_json("GET", f"{base}/contact-requests")
    assert st == 401, f"Expected 401 for anonymous GET, got {st}"
    checks += 1

    # 6. Non-admin GET -> 403
    resident_token = sign_in(base, args.resident_email, args.password)
    st, _, _ = http_json("GET", f"{base}/contact-requests", token=resident_token)
    assert st == 403, f"Expected 403 for resident GET, got {st}"
    checks += 1

    # 7. Admin GET list & GET by ID -> 200 containing persisted record
    admin_token = sign_in(base, args.admin_email, args.password)
    st, _, items = http_json("GET", f"{base}/contact-requests", token=admin_token)
    assert st == 200 and any(x["id"] == created["id"] for x in items), f"Admin list missing created item: {st} {items}"
    st, _, single = http_json("GET", f"{base}/contact-requests/{created['id']}", token=admin_token)
    assert st == 200 and single["email"] == "cmendoza@andina.pe", f"Admin get-by-id failed: {st} {single}"
    checks += 1

    # 8. OpenAPI spec exposes /api/v1/contact-requests (POST anonymous, GET secured)
    api_root = base[:-len("/api/v1")] if base.endswith("/api/v1") else base
    st, _, openapi = http_json("GET", f"{api_root}/openapi/v1.json")
    assert st == 200 and "/api/v1/contact-requests" in openapi.get("paths", {}), "OpenAPI missing /api/v1/contact-requests"
    path_item = openapi["paths"]["/api/v1/contact-requests"]
    assert not path_item["post"].get("security"), "POST /api/v1/contact-requests should be public in OpenAPI"
    assert path_item["get"].get("security"), "GET /api/v1/contact-requests should require Bearer in OpenAPI"
    checks += 1

    print(f"PASS: {checks} A04 contact-request HTTP & OpenAPI checks verified (created id={created['id']}).")


if __name__ == "__main__":
    main()
