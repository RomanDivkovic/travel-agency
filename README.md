# Personal Travel Agency & Booking Platform

A Swedish-first platform for one independent travel agent. Development follows [the master plan](docs/MASTER_PLAN.md), one phase at a time. This change implements **Phase 0: project foundation** only. Branding, customer requests, authentication, bookings, and payments are deferred.

## Stack

- React, strict TypeScript, Vite, React Router, SCSS Modules, and Axios.
- ASP.NET Core / C# on .NET 10 LTS, Entity Framework Core, and Npgsql.
- PostgreSQL 18 for local development through Docker Compose.
- ESLint, Prettier, Vitest, React Testing Library, xUnit, and GitHub Actions.

Exact frontend versions are in `apps/web/package.json` and its lockfile; backend versions are pinned in project files and NuGet lockfiles. Node is pinned by `.nvmrc`, and the .NET SDK by `global.json`.

## Structure

```text
apps/web/                         React frontend and tests
services/api/
  TravelAgency.Api/               HTTP, middleware, composition root
  TravelAgency.Application/       Application interfaces and future use cases
  TravelAgency.Domain/            Dependency-free domain project (no entities yet)
  TravelAgency.Infrastructure/    EF, PostgreSQL, migrations
  TravelAgency.Tests/             API and database integration tests
scripts/                         Local environment helpers
docs/                            Specification, architecture, development, decisions
.github/workflows/ci.yml          Frontend and backend CI
docker-compose.yml               Local PostgreSQL only
TravelAgency.sln                  Backend solution
```

## Prerequisites and quick start

Install Node 24 (see `.nvmrc`), the .NET SDK in `global.json`, and Docker Desktop with Compose v2. The environment helper uses Bash; on Windows use WSL or Git Bash. Start Docker Desktop first.

From the repository root:

```bash
node scripts/setup-env.mjs
docker compose up -d --wait postgres
dotnet restore TravelAgency.sln --locked-mode
dotnet tool restore
./scripts/with-env.sh dotnet ef database update --project services/api/TravelAgency.Infrastructure
npm --prefix apps/web ci
```

Start the API in one terminal:

```bash
./scripts/with-env.sh dotnet run --project services/api/TravelAgency.Api --no-launch-profile
```

Start the frontend in another:

```bash
npm --prefix apps/web run dev
```

Visit <http://localhost:5173>. The placeholder displays **API anslutet** when it receives a valid health response. This is a development screen, not the Phase 1 homepage.

API checks:

```bash
curl --fail http://localhost:5000/api/health
curl --fail http://localhost:5000/api/health/ready
```

Liveness verifies only the API process. Readiness verifies PostgreSQL connectivity. Both return `{"status":"healthy"}` when successful; readiness returns HTTP 503 and `{"status":"unhealthy"}` when the database is unavailable.

## Quality commands

```bash
npm --prefix apps/web run format:check
npm --prefix apps/web run lint
npm --prefix apps/web run typecheck
npm --prefix apps/web run test
npm --prefix apps/web run build
npm --prefix apps/web audit --audit-level=low
dotnet format TravelAgency.sln --verify-no-changes --no-restore
dotnet build TravelAgency.sln --configuration Release --no-restore -warnaserror
dotnet test --solution TravelAgency.sln --configuration Release --no-build --no-restore
dotnet list TravelAgency.sln package --vulnerable --include-transitive
```

The database integration test explicitly skips unless `TEST_DATABASE_CONNECTION` points to a dedicated database whose name ends in `_tests`. [Development instructions](docs/DEVELOPMENT.md) explain how to run it; CI enables it against an isolated database.

## Documentation and status

- [Architecture](docs/ARCHITECTURE.md)
- [Development, migrations, configuration, and troubleshooting](docs/DEVELOPMENT.md)
- [Architectural decisions](docs/DECISIONS.md)
- [Phase 0 verification and limitations](docs/PHASE_0_VERIFICATION.md)
- [Earlier requirements review](docs/REQUIREMENTS_REVIEW.md)

CI runs on pull requests and pushes to `main`. Deployment is outside Phase 0. Do not expose this development configuration as a production service or enable real bookings/payments before the master plan's launch gates are satisfied.
