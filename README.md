# Personal Travel Agency & Booking Platform

A Swedish-first platform for one independent travel agent. Development follows [the master plan](docs/MASTER_PLAN.md), one phase at a time. **Phase 8 adds manual travel research and versioned customer proposals, building on secure quotations and service-fee invoicing.** The Phase 5 administrator dashboard remains available. Phase 4 submission, PostgreSQL persistence and durable email notifications remain operational. The bilingual wizard receives a server-generated receipt only after saving. Local development captures emails without external delivery. Customer accounts, supplier bookings and live payment-provider integration remain deferred.

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
  TravelAgency.Application/       Request validation, submission use case, interfaces
  TravelAgency.Domain/            Dependency-free travel enquiry model
  TravelAgency.Infrastructure/    EF, PostgreSQL, migrations, outbox and email adapters
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

Visit <http://localhost:5173> for the homepage. The original API diagnostics remain at <http://localhost:5173/_dev/health> in development and display **API anslutet** when a valid health response is received. Diagnostics are excluded from production routing.

The provisional name is configured in `apps/web/src/app/config/branding.ts`. Public routes are `/`, `/services`, `/pricing`, `/about`, `/contact`, and `/faq`. `/about-preview` redirects to `/about`. `/plan-your-trip` now opens the travel request wizard. Service cards on the homepage and Services page link to it with a validated `?service=` preselection.

The header switches the whole public site between Swedish (default) and English without changing the route. Typed homepage resources are in `apps/web/src/content/sv.ts` and `en.ts`; page resources are in `content/public.ts`. The language choice is stored under `travel-agency.language.v1` in localStorage. Blocked storage falls back to in-memory switching.

The contact form is disabled until `VITE_CONTACT_EMAIL` contains an approved public mailbox. When configured, it validates fields and prepares an encoded `mailto:` draft for the visitor to review and send in their email application. **This general contact form does not deliver or store messages**, and opening a draft is never reported as delivery. Travel enquiries use the separate Phase 4 API and notification flow below. Configure only public values in `apps/web/.env`, then restart/rebuild; see the development guide.

Each route has localized title, description and social metadata. Prelaunch indexing remains disabled. Optional `VITE_SITE_URL` supplies an approved HTTPS origin for canonical links; there is no invented domain. Locale-specific URLs, hreflang, sitemap publication and prerendering remain launch work. Contact details, legal pages and the agent portrait remain explicit placeholders.

The wizard covers service selection, destination, fixed/flexible dates, travellers, budget, relevant preferences, additional requests, contact details and an editable review. It uses a typed reducer, pure step/whole-request validation and a discriminated TypeScript `TravelRequest` payload in `apps/web/src/features/travel-request`. All four service journeys work in Swedish and English. Explicit submission calls `POST /api/travel-requests`. Server validation, an atomic database transaction and an idempotency key protect retries. A confirmed HTTP 201 opens `/plan-your-trip/success` with the actual reference; email processing is separate and does not establish a booking.

Local drafts are **opt-in**. Only service, dates, traveller counts/ages, budget, structured preferences and activity-help choice are stored under `travel-agency.request-draft.v1`. Destinations, departure locations, contact details and free text are never persisted. Versioned drafts can be restored for seven days and are validated on read; expired/corrupt drafts are discarded on the next wizard visit. Restored sessions start at service selection and require re-entry of excluded details. Start over clears the draft and disables saving. A separate localStorage record stores only the submission UUID and creation time, never the payload. Retrying an uncertain response keeps that key. Confirmed success clears the draft and attempt; the receipt alone stays in sessionStorage for up to 24 hours. See the architecture and development guides for recovery and retention limits.

API checks:

```bash
curl --fail http://localhost:5080/api/health
curl --fail http://localhost:5080/api/health/ready
```

Liveness verifies only the API process. Readiness verifies PostgreSQL connectivity. Both return `{"status":"healthy"}` when successful; readiness returns HTTP 503 and `{"status":"unhealthy"}` when the database is unavailable.

## Administration

Open `/admin/login` after explicitly provisioning the initial administrator. Protected routes are `/admin`, `/admin/requests` and `/admin/requests/:id`. The dashboard shows real PostgreSQL statistics; the list uses server-side search, filters, pagination and sorting. Details show customer travel requirements, controlled status changes, private notes and activity history in Swedish/English.

Authentication uses ASP.NET Core Identity and HttpOnly cookies, independent API authorization, antiforgery tokens, lockout and login rate limiting. There is no public administrator registration or default account/password. See [the provisioning instructions](docs/DEVELOPMENT.md#administrator-provisioning) before attempting login. MFA, secure production key management and an operational credential-recovery process are production prerequisites; this phase does not claim production readiness.

## Service quotations

Administrators can create a quotation from an open enquiry, edit its draft, preview saved terms/prices, issue it, create revisions and withdraw eligible versions. `/admin/quotations` provides server-side search, status filters, sorting and pagination. Issued content is immutable; acceptance, decline and expiry are recorded without changing the enquiry status or starting paid work.

**Issuance is disabled by default.** Configure owner/adviser-approved tax treatment, rates, bilingual terms, acceptance wording and the approved frontend origin before enabling it. Drafts are usable without that approval. No legal terms or Swedish VAT rate are assumed. See [quotation configuration](docs/DEVELOPMENT.md#quotation-configuration-and-operation).

Customers receive a private `/quote#…` link scoped to one version. The fragment credential is sent to fixed public API routes through a header, never a request URL or browser storage. Customers can read the service fee/terms and explicitly accept or decline without an account. Expiry and revocation are enforced by the server. Acceptance records no payment, booking or automatic research activation. Local email capture remains the verified delivery boundary.

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

The database integration tests explicitly skip unless `TEST_DATABASE_CONNECTION` points to a dedicated database whose name ends in `_tests`. [Development instructions](docs/DEVELOPMENT.md) explain how to run it; CI enables it against an isolated database.

## Documentation and status

- [Architecture](docs/ARCHITECTURE.md)
- [Development, migrations, configuration, and troubleshooting](docs/DEVELOPMENT.md)
- [Architectural decisions](docs/DECISIONS.md)
- [Phase 0 verification and limitations](docs/PHASE_0_VERIFICATION.md)
- [Phase 1 completion and verification](docs/PHASE_1_COMPLETION.md)
- [Design system and visual references](docs/DESIGN_SYSTEM.md)
- [Image provenance and asset prompts](docs/ASSETS.md)
- [Earlier requirements review](docs/REQUIREMENTS_REVIEW.md)

CI runs on pull requests and pushes to `main`. Work always happens on a separate phase branch; merge only after explicit approval. Deployment is outside the current scope. The site retains prelaunch `noindex` and a disallowing `robots.txt`; revisit both before an approved public launch.

## Phase 7 — invoicing and service payments

Accepted quotations now support immutable invoice drafts/issuance, authorized downloadable HTML documents, secure invoice links, payment attempts, verified-event settlement, bounded reconciliation, overdue detection, bilingual customer/result pages, admin invoice management and currency-separated dashboard metrics. Existing auth/CSRF, outbox, localization and design components are reused.

**No live payment provider is approved or integrated.** Payments default to Disabled and commercial invoice issuance remains off. The Development-only Sandbox adapter explicitly simulates outcomes without charging money; simulated eligibility is separate from real travel-work eligibility. Real hosted checkout/provider sandbox acceptance remains blocked pending provider approval and implementation, alongside required business/tax/accounting/legal configuration. Nothing in this phase confirms supplier bookings.

Admin: `/admin/invoices`, `/admin/invoices/:id`; customers: `/invoice#credential`, `/payment/result#credential`; Development simulator: `/payment/sandbox#credential`. Accepted quotation details link to invoice creation. See the Phase 7 sections in [Development](docs/DEVELOPMENT.md), [Architecture](docs/ARCHITECTURE.md) and [Decisions](docs/DECISIONS.md) for configuration, security, testing and production blockers. Phase 8 research and proposals are documented below.

## Phase 8 — travel research and proposal builder

Real service-payment eligibility now gates a manual research workspace with flight legs/timezones, accommodation, extras, supplier conditions/source timestamps, private notes and side-by-side comparison. Build ordered proposal alternatives with explicit agent recommendations, currency-separated supplier estimates and a separate already-paid service fee. Sent snapshots are immutable; revisions preserve history and revoke old links. Customers explicitly select, acknowledge and confirm approval/rejection through a scoped private link. Existing admin auth/CSRF, transactional outbox, activity and background processing are reused.

Admin: `/admin/requests/:id/research`, `/admin/proposals`, `/admin/proposals/:id`, `/admin/proposals/:id/edit`. Customer: `/travel-proposal#credential`. Swedish and English are supported. Sending remains disabled until reviewed proposal terms/acknowledgement and a valid origin are configured. Sandbox payments cannot activate research; Phase 7's real provider remains unapproved/unintegrated. No supplier website is scraped, no availability is guaranteed and no supplier booking/payment or Phase 9 functionality is implemented.
