# Architectural decisions

## 001 — Modular monolith with explicit layers

**Decision:** One ASP.NET Core API with Api, Application, Domain, and Infrastructure projects; one React frontend; one PostgreSQL database.

**Reason:** A single operator needs maintainable boundaries without distributed-system overhead.

**Alternatives:** A single mixed project; microservices.

**Consequences:** Api owns composition; Domain remains independent, and Application cannot reference Infrastructure. The empty Domain project establishes the requested boundary without inventing travel models.

## 002 — Requested frontend stack and strict typing

**Decision:** React/Vite with declarative React Router, SCSS Modules, Axios, and strict TypeScript. Validate health JSON at runtime and centralize network error conversion.

**Reason:** Matches the explicit Phase 0 stack while keeping the placeholder small.

**Alternatives:** A full React framework, native fetch, utility CSS.

**Consequences:** Future public SEO may require prerendering or a deliberate rendering change. No speculative data-fetching framework, authentication interceptor, or design system is introduced.

## 003 — Stable pinned toolchains and package graphs

**Decision:** .NET 10 LTS, Node 24, PostgreSQL 18.6; exact frontend dependencies, NuGet lockfiles, a local pinned EF tool, and immutable action commits.

**Reason:** Reproducible local builds and CI with current stable compatible versions at implementation time.

**Alternatives:** Floating latest tags, preview .NET, older LTS versions close to end of support.

**Consequences:** Updates are intentional and include locks. EF runtime/design/tooling must share the same patch version; Npgsql's minimum EF version alone is insufficient to ensure alignment.

## 004 — Explicit migrations and independent health signals

**Decision:** Empty baseline migration, explicit migration commands, liveness independent of database connectivity, and a separate readiness check.

**Reason:** Verify the full infrastructure path without creating future-phase entities or taking the API down whenever PostgreSQL fails.

**Alternatives:** Auto-migrate at startup, EnsureCreated, a single health check, premature business tables.

**Consequences:** Developers/deployment must apply migrations separately. Readiness checks connectivity, while migration verification checks schema drift independently.

## 005 — Environment secrets and conservative HTTP defaults

**Decision:** Generated ignored local environment files; no fallback passwords; loopback database exposure; explicit development CORS only; safe Problem Details and basic security headers.

**Reason:** Avoid accidental credential publication, public database exposure, and information leakage.

**Alternatives:** Committed local passwords, wildcard CORS, unrestricted development hosts.

**Consequences:** ASP.NET commands require environment export. Production host, HTTPS proxy, secrets, and origin configuration are explicit future deployment tasks. Authentication is deliberately absent in Phase 0.

## 006 — Real database integration alongside isolated API tests

**Decision:** WebApplicationFactory tests with controlled readiness plus one optional PostgreSQL integration test. CI always supplies a dedicated test database.

**Reason:** Fast deterministic HTTP tests and evidence that the real provider and migrations work.

**Alternatives:** Mock-only testing, EF in-memory as a PostgreSQL substitute, mandatory Docker for every unit-test run.

**Consequences:** A plain local `dotnet test` explicitly skips the database test unless configured. Database names must end in `_tests`; tests never drop databases. No production secrets are needed in CI.

The .NET 10 SDK uses Microsoft Testing Platform through `global.json`, and the xUnit test project enables its native runner. Commands use `dotnet test --solution TravelAgency.sln`; VSTest compatibility dependencies are omitted because this project standardizes on .NET 10.

## Research references

Official references consulted during implementation:

- [Vite guide](https://vite.dev/guide/) and [SCSS support](https://vite.dev/guide/features#css-pre-processors)
- [React Router installation](https://reactrouter.com/start/declarative/installation)
- [Axios instances](https://axios-http.com/docs/instance) and [error handling](https://axios-http.com/docs/handling_errors)
- [Vitest](https://vitest.dev/guide/) and [Testing Library setup](https://testing-library.com/docs/react-testing-library/setup/)
- [ESLint configuration](https://eslint.org/docs/latest/use/configure/configuration-files) and [Prettier installation](https://prettier.io/docs/install)
- [ASP.NET integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [EF migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/) and [Npgsql EF provider](https://www.npgsql.org/efcore/)
- [PostgreSQL support policy](https://www.postgresql.org/support/versioning/) and [official container](https://hub.docker.com/_/postgres)
- [Compose variable interpolation](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/)
