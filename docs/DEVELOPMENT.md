# Development

## Phase branch workflow

Never implement changes directly on `main`. Before each phase, inspect Git status and preserve any existing work. When the previous phase is merged and the working tree is clean, switch to `main`, pull with `git pull --ff-only origin main`, then create the assigned phase branch before editing files. Phase 1 uses `phase-1-branding-homepage`.

Do not discard changes, force-push, delete unmerged branches, or merge a phase automatically. Staging and committing require explicit user authorization. Keep each phase reviewable so it can be tested independently or rolled back through Git history.

## Initial setup

Use Node 24.19 or later within major 24 (`nvm use` reads `.nvmrc`), .NET SDK 10.0.402 or a compatible later patch in that feature band, Docker Desktop, and Docker Compose v2. Bash is required for `scripts/with-env.sh` (WSL/Git Bash on Windows).

From the repository root:

```bash
node scripts/setup-env.mjs
npm --prefix apps/web ci
dotnet restore TravelAgency.sln --locked-mode
dotnet tool restore
docker compose up -d --wait postgres
./scripts/with-env.sh dotnet ef database update --project services/api/TravelAgency.Infrastructure
```

The setup script preserves existing `.env` files. On first use it copies the examples and substitutes a cryptographically generated local password. It never prints credentials. Review local files if you customized them previously; rerunning setup deliberately does not repair or overwrite them.

## Configuration

| Variable | Consumer | Purpose |
| --- | --- | --- |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Compose | Development database and credentials |
| `POSTGRES_PORT` | Compose | Loopback host port (default 5432) |
| `ConnectionStrings__TravelAgency` | API and EF | Npgsql connection string; required, no credential fallback |
| `ASPNETCORE_ENVIRONMENT` | API | `Development` locally; `Production` on deployed infrastructure |
| `ASPNETCORE_URLS` | API | Local listener, `http://localhost:5000` |
| `Cors__AllowedOrigins__0` | API | Explicit development browser origin, `http://localhost:5173` |
| `VITE_API_BASE_URL` | Vite | Public API URL, `http://localhost:5000` locally |
| `TEST_DATABASE_CONNECTION` | xUnit | Optional dedicated database connection; database name must end in `_tests` |

ASP.NET Core does not automatically read `.env`. `scripts/with-env.sh` exports the root file before executing its command. This file is trusted shell configuration: do not source an untrusted `.env`, use it in shared logs, or enable shell tracing. Vite reads `apps/web/.env` independently. Restart dev processes after changing configuration. Changing `POSTGRES_PORT`, database name, username, or password also requires updating the API connection string.

Only examples belong in Git. Production must use managed environment/secret configuration, a private database, TLS, explicit public host settings (`AllowedHosts`), and an intentional origin/reverse-proxy policy. `VITE_` values are included in browser code and must never contain secrets. The local Compose file is not a production deployment.

## Running

```bash
# Repository root, terminal 1
./scripts/with-env.sh dotnet run --project services/api/TravelAgency.Api --no-launch-profile
```

```bash
# Repository root, terminal 2
npm --prefix apps/web run dev
```

Open <http://localhost:5173>. The API listens on <http://localhost:5000>. Vite refuses a busy port instead of silently switching to an origin that CORS does not allow. Use `npm --prefix apps/web run preview` after a frontend build for a local build preview; configure its origin explicitly if testing API calls on another port.

The public homepage makes no health requests. Use `/_dev/health` on the development server for API diagnostics; that route is absent from the production build. `/plan-your-trip` and `/about-preview` are informational fallbacks for future phases, not functioning forms or full public pages. Use homepage anchors for current services/process/about/contact navigation.

```bash
curl -i http://localhost:5000/api/health
curl -i http://localhost:5000/api/health/ready
docker compose ps
docker compose logs --tail=50 postgres
```

## Migrations

The initial `Baseline` migration intentionally contains no business schema. EF creates only its own history table. Run migrations explicitly; API startup does not mutate the database.

```bash
./scripts/with-env.sh dotnet ef migrations list --project services/api/TravelAgency.Infrastructure
./scripts/with-env.sh dotnet ef database update --project services/api/TravelAgency.Infrastructure
./scripts/with-env.sh dotnet ef migrations has-pending-model-changes --project services/api/TravelAgency.Infrastructure
```

In a future authorized phase, after changing the EF model:

```bash
./scripts/with-env.sh dotnet ef migrations add DescriptiveName --project services/api/TravelAgency.Infrastructure --output-dir Persistence/Migrations
```

Review generated migrations before applying them. Never replace a migration already applied in shared environments. Generate and review deployment scripts during the deployment phase.

## Tests and quality

```bash
npm --prefix apps/web run format:check
npm --prefix apps/web run lint
npm --prefix apps/web run typecheck
npm --prefix apps/web run test
npm --prefix apps/web run build
dotnet format TravelAgency.sln --no-restore --verify-no-changes
dotnet build TravelAgency.sln --configuration Release --no-restore -warnaserror
dotnet test --solution TravelAgency.sln --configuration Release --no-build --no-restore
```

Use `npm --prefix apps/web run test:watch` for frontend watch mode. Fix formatting with `npm --prefix apps/web run format` and `dotnet format TravelAgency.sln --no-restore`.

Backend tests use in-process test hosting; no running API is required. They simulate readiness success/failure and also exercise an unreachable real PostgreSQL connection. Without `TEST_DATABASE_CONNECTION`, the database migration integration test is explicitly skipped.

`global.json` selects Microsoft Testing Platform, as required by the current xUnit package on .NET 10. Use `dotnet test --solution TravelAgency.sln` rather than the legacy positional solution argument. No VSTest compatibility packages are needed for this SDK baseline.

To run the real integration test locally, first create the dedicated database (once):

```bash
docker compose exec -T postgres sh -c 'createdb -U "$POSTGRES_USER" travel_agency_tests'
```

Then run:

```bash
./scripts/with-env.sh bash -c 'export TEST_DATABASE_CONNECTION="Host=127.0.0.1;Port=$POSTGRES_PORT;Database=travel_agency_tests;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD;Timeout=3;Command Timeout=5"; dotnet test --solution TravelAgency.sln'
```

This test applies migrations twice to that isolated database; it does not drop a database or touch the application database. A database name without the `_tests` suffix fails before accessing it. The local example uses the development cluster; CI uses its own ephemeral container.

Dependency checks:

```bash
npm --prefix apps/web audit --audit-level=low
dotnet restore TravelAgency.sln --locked-mode -warnaserror
dotnet list TravelAgency.sln package --vulnerable --include-transitive
```

NuGet audit checks transitive dependencies during restore; warnings fail under `-warnaserror`. The package-list command is a human-readable report and does not itself enforce a failing exit code for vulnerabilities. Commit updated lockfiles with intentional dependency changes; normal setup/CI uses locked restores and `npm ci`.

## Database lifecycle and safe reset

`docker compose stop postgres` stops PostgreSQL without deleting data. `docker compose start postgres` restarts it. `docker compose down` removes this project's container/network while preserving the volume.

For a **deliberate reset of this project's local development data only**, stop the API and confirm that the selected Docker context and Compose project are your local development environment:

```bash
docker context show
docker compose ps
# Destructive: deletes this Compose project's database volume, including test data.
docker compose down --volumes
docker compose up -d --wait postgres
./scripts/with-env.sh dotnet ef database update --project services/api/TravelAgency.Infrastructure
```

Recreate `travel_agency_tests` afterward if needed. Never use these reset commands against shared or production databases. Changing an existing PostgreSQL container's password variable does not change the password stored in the database; update it deliberately or reset only disposable local data.

## Troubleshooting

- **Docker socket unavailable:** start Docker Desktop and wait for `docker info` to succeed.
- **Missing connection string:** use `scripts/with-env.sh`, or export configuration explicitly. The API fails fast on missing configuration.
- **Readiness 503 while liveness is 200:** check container health, port, database name, credentials, and connection-string consistency. Public health output deliberately omits internal details.
- **API unavailable in the browser:** start the API, confirm `VITE_API_BASE_URL`, and use exactly `http://localhost:5173` unless the allowed development origin was changed. `127.0.0.1` and `localhost` are distinct browser origins.
- **Database port busy:** choose another `POSTGRES_PORT` and update `ConnectionStrings__TravelAgency`; restart Compose and the API.
- **SDK not found:** install the SDK specified in `global.json`; do not silently switch to a preview SDK.
- **EF package conflict:** keep EF runtime, design package, and dotnet-ef on matching 10.0 patch versions. Npgsql's lower minimum dependency is not a reason to downgrade the explicitly pinned runtime.
- **Integration test skipped:** create the isolated test database and set `TEST_DATABASE_CONNECTION` as shown above.
- **No GitHub CI run:** configure a remote, push the review branch, and open a PR. This phase does not create a remote, stage files, commit, push, or merge automatically.
