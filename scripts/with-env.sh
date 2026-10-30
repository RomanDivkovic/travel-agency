#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ ! -f "$project_root/.env" ]]; then
  echo "Missing .env. Follow docs/DEVELOPMENT.md to configure local development." >&2
  exit 1
fi
# The local .env is trusted shell configuration; never source an untrusted file.
set -a
source "$project_root/.env"
set +a
cd "$project_root"
exec "$@"
