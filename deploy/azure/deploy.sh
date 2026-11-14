#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")"

if [[ ! -f .env ]]; then
    echo 'Create deploy/azure/.env from .env.example and fill in the deployment values.' >&2
    exit 1
fi
if grep -q 'replace-with-' .env; then
    echo 'Replace every placeholder in .env before deploying.' >&2
    exit 1
fi

docker compose config --quiet
docker compose build --pull api
docker compose up -d --wait mysql
docker compose --profile tools run --rm --no-deps migrate
docker compose --profile tools run --rm --no-deps bootstrap-admin
docker compose up -d api caddy
docker compose ps
