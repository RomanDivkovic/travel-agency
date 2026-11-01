# Development

## Phase branch workflow

Never implement changes directly on `main`. Before each phase, inspect Git status and preserve any existing work. When the previous phase is merged and the working tree is clean, switch to `main`, pull with `git pull --ff-only origin main`, then create the assigned phase branch before editing files. Phase 4 uses `phase-4-request-persistence-notifications`.

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
| `ASPNETCORE_URLS` | API | Local listener, `http://localhost:5080` |
| `Cors__AllowedOrigins__0` | API | Explicit development browser origin, `http://localhost:5173` |
| `VITE_API_BASE_URL` | Vite | Public API URL, `http://localhost:5080` locally |
| `VITE_CONTACT_EMAIL` | Vite | Optional approved public mailbox; blank or invalid disables the contact form |
| `VITE_SITE_URL` | Vite | Optional public HTTPS origin for canonical links; never a secret |
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

Open <http://localhost:5173>. The API listens on <http://localhost:5080>. Vite refuses a busy port instead of silently switching to an origin that CORS does not allow. Use `npm --prefix apps/web run preview` after a frontend build for a local build preview; configure its origin explicitly if testing API calls on another port.

The public homepage makes no health requests. Use `/_dev/health` on the development server for API diagnostics; that route is absent from the production build. Public pages are `/services`, `/pricing`, `/about`, `/contact` and `/faq`, in addition to `/`. `/about-preview` redirects to `/about`. `/plan-your-trip` opens the travel wizard; explicit submission is available when the API and database are running. Existing homepage anchors (including `/#sa-fungerar-det`) are preserved.

### Language and contact verification

Choose Svenska or English in the header, navigate to another page, then reload. All page copy, image alternatives, metadata and document language should follow the selection. The preference is local to the browser origin, under `travel-agency.language.v1`; clear that key to test the Swedish default. Invalid or inaccessible storage falls back safely. Keep shared translation keys and stable IDs in sync when editing `src/content/sv.ts`, `en.ts` and `public.ts`.

Leave `VITE_CONTACT_EMAIL` blank until a real mailbox is approved. To verify the configured contact flow locally, set a test mailbox in `apps/web/.env`, restart Vite, fill the required fields and prepare the draft. Confirm that an encoded mailto link appears and that no delivery confirmation is displayed. Open the link only when you intend to use a local email client; actual sending happens there. No message is posted to the API or saved by the website. Remove the test address afterward. Vite exposes these values in the browser, so never use provider API keys here.

`VITE_SITE_URL` is optional until the public domain is approved. Use an HTTPS origin to emit path-specific canonical links. Localized client metadata is implemented, but noindex/robots restrictions remain deliberate. Launch requires reviewing indexing, legal content, multilingual URL strategy and static-host SPA fallback routing (all public paths must serve index.html). No SSR, sitemap or hreflang claim is made in Phase 2.

```bash
curl -i http://localhost:5080/api/health
curl -i http://localhost:5080/api/health/ready
docker compose ps
docker compose logs --tail=50 postgres
```

## Travel wizard development and verification

Use the repository root for `npm --prefix apps/web ...` commands, or change into `apps/web` and run `npm ...` directly. Editing remains local; submitting requires the API, migrated PostgreSQL database and the Phase 4 configuration below.

Open `/plan-your-trip` for generic service selection, or use `?service=flights`, `accommodation`, `flight-accommodation` or `complete` to preselect a service. Unknown values are ignored. Homepage and Services-page cards use the shared mapping in `features/travel-request/config.ts`. The flow is configured by service, not by translated labels or hardcoded numeric step indexes.

For a manual journey, select a service; enter a departure location when flights apply; enter a destination or opt into suggestions; set fixed/flexible dates; enter travellers/child ages and rooms when applicable; set a positive budget; choose relevant preferences; add optional requests; enter contact details; and review. Try blank/invalid fields to confirm progression is blocked. Use Back to retain answers, or an Edit button in review and Back to review to validate all answers again. Changing service resets service-specific choices. Switch languages mid-step and confirm values and translated errors are preserved. Use Send only with synthetic data during verification. Confirm that HTTP 201 precedes the receipt screen; a pending or failed request must retain answers and permitted drafts.

Draft saving is optional. Tick the save checkbox and inspect `travel-agency.request-draft.v1` in browser storage: it must contain only service, dates, traveller counts/ages, budget, structured preferences and an activity-help boolean. It must not contain contact, destination/origin/stops or special-request text. Reload to restore permitted answers, then re-enter omitted details. A stored draft restores at service selection rather than jumping over validation. Invalid/expired drafts are discarded on mount. Confirm that Start over clears this key and unchecks saving without changing the separate language preference. Blocked storage must produce feedback and still allow the journey in memory.

`model.ts` holds input and payload types; `reducer.ts` handles answer updates and service/child-count transitions; `validation.ts` is shared by step gating and payload construction; `payload.ts` is the Phase 4 integration boundary. The dedicated submission service posts the payload only after explicit Send; browser drafts never contain it. Extend client and independent server validation tests together when conditional rules change.

## Migrations

The initial `Baseline` remains empty. The additive `TravelRequestSubmission` migration creates enquiry, idempotency and outbox tables plus the reference sequence. Run migrations explicitly; API startup does not mutate the database.

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

Backend tests use in-process test hosting; no running API is required. They simulate readiness success/failure and also exercise an unreachable real PostgreSQL connection. Without `TEST_DATABASE_CONNECTION`, the PostgreSQL integration tests are explicitly skipped.

`global.json` selects Microsoft Testing Platform, as required by the current xUnit package on .NET 10. Use `dotnet test --solution TravelAgency.sln` rather than the legacy positional solution argument. No VSTest compatibility packages are needed for this SDK baseline.

To run the real integration test locally, first create the dedicated database (once):

```bash
docker compose exec -T postgres sh -c 'createdb -U "$POSTGRES_USER" travel_agency_tests'
```

Then run:

```bash
./scripts/with-env.sh bash -c 'export TEST_DATABASE_CONNECTION="Host=127.0.0.1;Port=$POSTGRES_PORT;Database=travel_agency_tests;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD;Timeout=3;Command Timeout=5"; dotnet test --solution TravelAgency.sln'
```

These tests apply migrations and exercise real transactions/concurrency against that isolated database; it does not drop a database or touch the application database. A database name without the `_tests` suffix fails before accessing it. The local example uses the development cluster; CI uses its own ephemeral container.

Dependency checks:

```bash
npm --prefix apps/web audit --audit-level=low
dotnet restore TravelAgency.sln --locked-mode -warnaserror
dotnet list TravelAgency.sln package --vulnerable --include-transitive
```

NuGet audit checks transitive dependencies during restore; warnings fail under `-warnaserror`. The package-list command is a human-readable report and does not itself enforce a failing exit code for vulnerabilities. Commit updated lockfiles with intentional dependency changes; normal setup/CI uses locked restores and `npm ci`.

## Phase 2 verification — 2026-11-01

- Frontend working directory: `apps/web` (root commands use `npm --prefix apps/web`). Formatting, ESLint, strict type checking, all 42 Vitest tests across 8 files, production build and npm audit passed. Audit reported zero vulnerabilities. No dependency versions or backend code changed.
- Playwright Chromium inspected the actual development app at `http://localhost:5173`: 96 combinations of 8 routes, both languages and widths 320, 375, 768, 1024, 1280 and 1440px. Checks covered meaningful content, one main heading, metadata, document language, overflow, image loading, internal links, framework overlays and runtime/resource errors. No relevant errors or broken resources were detected.
- Keyboard and interaction checks passed for FAQ Enter/Space disclosure, desktop/mobile navigation, Escape focus restoration, skip-to-main, route/anchor focus, sticky-header clearance, the legacy about redirect, language switching and reload persistence. The mobile pricing comparison supports keyboard scrolling. Desktop/mobile screenshots were visually reviewed; these checks are not a formal assistive-technology certification or cross-browser audit.
- Axe ran 34 rendered checks across Swedish/English, desktop/mobile, open FAQ/menu states and configured contact. One repeated eyebrow-text contrast issue was fixed; the final run detected zero violations. Off-screen pricing-table cells generated two incomplete contrast checks; their rendered colors were reviewed separately (text contrast 12.56:1 on the page background and 10.65:1 on the table heading background).
- Contact default mode was verified disabled. An isolated Vite instance on `http://localhost:5174` with a fixture mailbox verified required fields, invalid email handling, encoded draft content, explicit no-delivery feedback and draft invalidation after edits. No email was sent and actual mailbox delivery remains unverified.
- Production preview at `http://localhost:4173` passed 8 direct-route smoke checks, language persistence, noindex metadata and absence of public health API calls. Development diagnostics correctly rendered the not-found page in production.
- Backend working directory: repository root. `dotnet format`, Release build (zero warnings/errors) and tests passed: 9 succeeded, 1 explicitly skipped because no dedicated `TEST_DATABASE_CONNECTION` was configured. The skipped PostgreSQL integration test is not claimed as executed.
- Browser plugin was not available; the existing Playwright and axe runtimes were used without adding project dependencies. Temporary browser scripts, screenshots and machine-readable evidence were kept outside the repository under `/private/tmp/travel-phase2-*`.

## Phase 3 verification — 2026-11-01

- From the repository root, `npm --prefix apps/web run format:check`, `lint`, `typecheck`, `test` and `build` passed. All 83 frontend tests across 11 files passed, covering the existing application plus four complete wizard journeys, payload filtering/normalization, validation boundaries, service/child-count transitions, edits/back navigation, language changes, draft allowlisting, corruption/expiry, storage errors and reset. The focus-race regression and changed-service URL/restoration regression are covered. `npm audit --audit-level=low` reported zero vulnerabilities; dependency files are unchanged.
- The Browser plugin was unavailable. The existing Playwright Chromium/axe runtimes inspected the actual app at `http://localhost:5173`, without adding dependencies. All four services completed to review in both languages (8 journeys). Fixed/one-way and flexible dates, flexible destinations, child ages, rooms, budget, dynamic preferences, activities, contact validation, review edits, Back navigation, draft privacy/restoration/reset, cancellation focus and language switching were exercised. All eight service-specific links across the homepage and Services page preselected correctly; the generic hero CTA opened the wizard.
- Wizard reflow was checked at 320, 375, 768, 1024 and 1440px, with full accommodation/complete journeys on mobile and flight/combined journeys on desktop. Eight existing public/redirect/not-found routes were checked at 320, 375 and 1440px (24 route/viewport combinations), including meaningful content, overflow, image loading and framework-overlay checks. No console/page/resource errors or broken routes/images were observed. No API traffic occurred during the public/wizard journeys.
- Twelve axe scans covered all step families plus review and reset confirmation. Zero violations or incomplete checks were reported. Keyboard/native control behavior, error links, heading/error focus, mobile/desktop screenshots and start-over cancellation focus were checked. This is Chromium and automated/manual keyboard QA, not a formal screen-reader or cross-browser conformance certification.
- Production preview at `http://localhost:4173` verified all four service query values, a complete one-way flight journey, editable contact review, disabled submission, noindex metadata, language persistence, permitted draft restoration, changed-service URL/refresh behavior, start-over and exclusion of development diagnostics. No console errors or API traffic occurred. Temporary preview processes were stopped afterward; browser evidence/scripts were kept outside the repository under `/private/tmp/travel-phase3-*`.
- From the repository root, backend formatting and Release build passed with zero warnings/errors. Backend tests: 9 passed, 1 explicitly skipped because no isolated `TEST_DATABASE_CONNECTION` was configured. The PostgreSQL integration test is not claimed as executed. Backend code, migrations and CI configuration are unchanged.
- Phase 3 implements request preparation only. No actual submission/delivery or server persistence was implemented or tested. Phase 4 remains unstarted.

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

## Phase 4 configuration and operation

API examples now use port **5080**; macOS can reserve port 5000 for Control Center. Keep root `ASPNETCORE_URLS` and `apps/web/.env` `VITE_API_BASE_URL` aligned. Do not terminate unrelated processes to obtain a port. Existing environment files are preserved by setup, so copy new safe settings from `.env.example` manually when upgrading.

| Configuration | Default / behavior |
| --- | --- |
| `Requests__ReferencePrefix` | `TRV`; uppercase 2–8 characters |
| `Requests__IdempotencyRetentionDays` | 30; minimum 7 |
| `Requests__RequestRetentionDays` | 365; explicit production value required |
| `Requests__PurgeExpiredRequests` | false; approve retention before enabling deletion |
| `Requests__PermitLimit`, `Requests__WindowSeconds` | 10 requests per IP / 60 seconds per instance |
| `Email__Provider` | `Capture` in development; `Resend` for external transport |
| `Email__BusinessName` | Provisional `Your Travel Planner` |
| `Email__FromAddress`, `Email__AgentRecipient` | Local `.invalid` fixtures; approved real mailboxes required for Resend |
| `Email__ApiKey` | Server secret required for Resend; never a Vite variable |
| `Email__CaptureDirectory` | `.local-mail`, relative to API content root |
| `Email__MaxAttempts`, `Email__PollSeconds` | 8 attempts, 5-second polling |
| `Email__WorkerEnabled` | true; false also disables periodic retention maintenance |

Capture performs no network delivery. The default files are in `services/api/TravelAgency.Api/.local-mail`, excluded from Git and owner-readable on Unix. Inspect only your synthetic fixtures; the files contain personal data. Successful jobs have `SentAt`, which means captured/accepted by transport. Customer and agent jobs are distinct. To verify capture: submit a synthetic enquiry, confirm its server receipt, then check two matching outbox rows and their GUID-named JSON files. Verify reference, recipient, Swedish/English customer text, HTML escaping and plain-text alternatives. Do not report capture as inbox delivery.

For external delivery, obtain an owner-approved Resend account, verified sender/domain, agent mailbox and secret API key. Set `Email__Provider=Resend`, the approved addresses and `Email__ApiKey` in server secret configuration. Restart the API. Test with an approved recipient and inspect provider status and the actual inbox. This has not been performed locally; no external credentials were supplied. Production rejects Capture and placeholder mailboxes. Do not log provider response bodies or secrets.

Request persistence commits before email processing. Temporary failures retry with bounded backoff; permanent failures and exhausted jobs have `DeadLetterAt`. There is no administrator UI yet. An authorized operator can inspect job IDs/kinds/attempts/timestamps and correct transport configuration; any manual requeue must target reviewed job IDs and consider duplicate delivery. Do not delete/recreate the enquiry to resend mail. Provider deduplication lasts 24 hours; outbox retries do not promise exactly-once delivery.

Hourly maintenance removes expired idempotency records. Optional request purging deletes old enquiries and cascades their keys/jobs; it is disabled by default. Approve privacy notices, legal basis and retention before launch, including separate capture-file, backup and provider deletion policies. Configure a private database, TLS, approved hosts/origins and trusted proxy handling before deployment. The per-process limiter requires infrastructure-level coordination for multiple API instances.

Submission recovery uses the same UUID after a lost response; the server returns the original receipt. A changed payload with that key returns 409. Restore the original answers or explicitly Start over after considering whether the first enquiry already exists. The browser permits attempts for seven days; server keys last 30 days by default. Clearing site data, resetting an attempt, or retrying after server expiry can create another enquiry. Blocked attempt storage prevents sending safely; opt-in draft storage alone is not required. The success page confirms persistence only and holds a receipt without contact information in tab-scoped storage for up to 24 hours.

### Phase 4 verification — 2026-11-01

- Frontend: 93 tests across 12 files; backend: 26 tests with real isolated PostgreSQL, none skipped. Tests cover existing routes and wizard behavior plus validation, concurrency, replay conflicts, transaction rollback, retry/dead letters, rate limiting, uncertain responses, receipt guards, storage failures and translation states.
- Playwright Chromium exercised all four services in Swedish and English against the real API on 5080 and the development database: eight unique saved enquiries. One deliberately lost response retried with the same key and returned the identical original receipt. No unexpected page/console errors occurred.
- Four axe scans (review mobile, uncertain-response error, Swedish mobile confirmation and English desktop confirmation) found zero violations or incomplete checks. Confirmation layouts were checked at 320, 375, 768, 1024 and 1440px; direct confirmation access without receipt and reload restoration were verified. Desktop/mobile screenshots were inspected. This does not establish screen-reader or cross-browser certification.
- Both migrations were applied to the existing development database without resets and to the isolated `travel_agency_phase4_tests` database. Temporary evidence is under `/private/tmp/travel-phase4-*`. Local capture is the email verification boundary; real external delivery remains unverified.

- Final quality checks passed: frontend Prettier, ESLint, TypeScript, 93 tests and production build; backend formatting, Release build with zero warnings/errors and 26 tests with none skipped. npm audit reported zero vulnerabilities; NuGet reported no vulnerable direct/transitive packages. Both default and Release EF model-drift checks passed. An initial default EF check stopped with a build failure; its isolated rerun passed without source changes.
- Additional rendered regression checks passed for eight public/wizard/not-found paths × two languages × two widths (32 combinations), including language persistence, FAQ disclosure, main headings, image loading and overflow. No unexpected console/page errors occurred. PostgreSQL confirmed two processed jobs per browser enquiry; all sixteen capture files had matching references, HTML/plain text, localized customer subjects and owner-only Unix permissions. No external email was sent.
