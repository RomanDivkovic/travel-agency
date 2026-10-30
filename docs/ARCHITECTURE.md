# Architecture

## Scope

Phase 0 creates a modular monolith with a separate React frontend and a single ASP.NET Core API backed by PostgreSQL. There are no microservices, background queues, customer models, authentication, or payment integrations. The Domain project intentionally contains no speculative entities.

## Dependency direction

```text
Domain ← Application ← Infrastructure
              ↑              ↑
              └──── Api ─────┘
Tests → Api / Application / Infrastructure
```

Domain references no other project. Application references Domain and defines the database-readiness abstraction. Infrastructure references Application and implements that abstraction with EF/Npgsql. Api is the composition root and references Application and Infrastructure. There are no circular references.

Application and Domain stay free of ASP.NET Core and EF dependencies. Future workflows belong in Application; domain rules belong in Domain; HTTP mapping stays thin. Add validation when concrete request contracts are introduced, rather than installing a validation framework with no current use.

## Frontend

`main.tsx` initializes React, a top-level error boundary, BrowserRouter, and global styles. `app/App.tsx` declares the landing and not-found routes within a shared layout. `FoundationPage` uses a focused health hook rather than embedding network handling in the view.

`services/apiClient.ts` centralizes the Axios base URL, timeout, and safe error conversion. `services/health.ts` validates the runtime health payload instead of trusting a TypeScript cast. The hook cancels in-flight work on unmount and handles rejections. SCSS Modules own layout styling; global styles cover typography and basic document defaults.

`VITE_API_BASE_URL` is public configuration embedded at build time. If unset, Axios uses same-origin URLs, suitable for a future reverse proxy. Local development uses the explicit URL in `apps/web/.env`. There is no production API host hardcoded in components.

Swedish is the placeholder UI language. Complete bilingual public content and SEO are Phase 2 concerns. This SPA foundation does not settle future prerendering/SSR requirements; revisit those for public-page SEO before production deployment.

## API and persistence

- `/api/health`: liveness without database access; HTTP 200 while the process is responsive.
- `/api/health/ready`: Npgsql connectivity through EF; HTTP 200 or 503. No exception or connection details appear in its JSON response. Connectivity is not proof that future schema migrations are current; migration checks are separate.
- Errors use ASP.NET Core Problem Details. Production responses do not expose stack traces; logs remain server-side. Unknown routes return Problem Details too.
- Development CORS permits configured loopback origins only. Production does not register this policy; a future production origin must be approved and explicitly configured when deployment is implemented.
- The API adds `nosniff`, `DENY` frame protection, and a no-referrer policy. Production uses HTTPS redirection and HSTS. A future reverse proxy must configure trusted forwarded headers and HTTPS correctly.

`TravelAgencyDbContext` has no business tables. The baseline migration establishes EF migration history only. Migrations run explicitly through the pinned EF tool; API startup never applies migrations automatically. Design-time tooling reads an environment connection string without inventing fallback credentials.

PostgreSQL Compose configuration is local-only: loopback port binding, persistent named volume, generated local password, and container health check. It is not production infrastructure.

## Verification boundaries

Vitest/Testing Library verify routes, pending/success/failure health states, runtime response validation, and error-boundary recovery. xUnit/WebApplicationFactory verify startup, DI, liveness, readiness success/failure, CORS, safe responses, and headers. The real database test applies the baseline twice against a dedicated test database and checks readiness.

CI uses pinned action commits, dependency lockfiles, isolated PostgreSQL, formatting/linting/type checks, builds, tests, migration checks, and package audits. No production secrets are required.
