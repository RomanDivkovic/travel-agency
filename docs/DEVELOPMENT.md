# Development

## Phase branch workflow

Never implement changes directly on `main`. Before each phase, inspect Git status and preserve any existing work. When the previous phase is merged and the working tree is clean, switch to `main`, pull with `git pull --ff-only origin main`, then create the assigned phase branch before editing files. Phase 6 uses `codex/phase-6-quotations-customer-approval`.

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

## Administrator provisioning

Apply migrations first, then run the explicit command below from the repository root. There is no startup seed, default password or public registration. Supply a real owner-approved email and display name. Read the initial password into the environment without placing it in shell history; the example uses Bash's hidden prompt (run in Bash on systems whose default shell differs):

```bash
# Bash, repository root; .env already contains the development database settings.
read -r -p "Administrator email: " Admin__Email
read -r -p "Display name: " Admin__DisplayName
read -r -s -p "Initial password (12+ characters, upper/lower/digit/symbol): " Admin__InitialPassword
export Admin__Email Admin__DisplayName Admin__InitialPassword
./scripts/with-env.sh dotnet run --project services/api/TravelAgency.Api --no-launch-profile -- --provision-admin
unset Admin__InitialPassword Admin__Email Admin__DisplayName
```

Do not paste passwords into chat, commands, Git, Vite variables or shared logs. Managed production secret injection should replace an interactive prompt. The command uses Identity hashing and validation, a database transaction and advisory lock. A repeat for the existing administrator reports no changes and never overwrites credentials; a different existing account prevents creating additional administrators. This phase intentionally has no account-management UI or password recovery endpoint. Recovery/rotation requires a reviewed operator procedure using Identity; do not edit password hashes directly. Never invent a production default account.

Start the API and frontend normally, then visit `/admin/login`. No administrator account is automatically created in the development database. Configure `Admin__SessionMinutes` (default 30, range 1–120) if needed; restart the API after environment changes. Changing the session lifetime affects newly issued cookies. The source example contains only the nonsecret session setting. Configure provisioning secrets transiently, not as permanent runtime requirements.

### Admin development and API checks

The standard local pair is frontend `http://localhost:5173` and API `http://localhost:5080`. Use the same hostname, include credentials, and allow only that exact origin. The admin adapter handles HttpOnly cookies automatically and fetches a fresh framework antiforgery token for each login/logout/status/note mutation. Raw mutation clients must first GET `/api/admin/auth/csrf`, retain its cookie, then send the returned token in `X-CSRF-TOKEN`; refresh it after login. Public Phase 4 submission remains independent, with its existing idempotency header.

After login, verify `/admin`, `/admin/requests` and a UUID detail path. Search reference/name/email, combine status/service/submission-date filters and change deterministic sort/page size. Filters are applied on explicit Search, keeping private search terms out of navigation history. Submission date filters are inclusive UTC dates. List page size is bounded at 100; notes/activity use chronological 50-item pages. A missing request shows a 404 state; a forbidden role shows a permission error. See the architecture endpoint table for response contracts.

Statuses are `Submitted`, `UnderReview`, `AwaitingCustomer`, `Closed`. The detail response supplies allowed next statuses and a version UUID. Status writes must include that version. A stale/concurrent change returns 409; reload and review the latest state rather than retrying blindly. Closed cannot reopen. Notes are plain text, at most 4000 characters, with author derived from the session. Notes and history must never appear in public responses or customer emails. Mutation success reloads data and activity; it does not send a customer message.

Verify login failures without revealing whether an address exists, five-failure lockout (15 minutes), ten-attempt IP rate limit (one minute), missing/invalid antiforgery rejection, anonymous 401 and nonadministrator 403. Logout revokes all administrator sessions through Identity's security stamp. Sessions expire absolutely after the configured time; the UI checks on focus/every minute and reacts immediately to a protected API 401. Browser back/refresh must not restore usable private data after logout. Never store administrator tokens, passwords, notes or response caches in browser storage.

The additive `AdminDashboard` migration upgrades the Phase 4 schema and backfills submitted-history events and concurrency versions without resetting data. Test databases must still end in `_tests`; existing tests apply migrations and clean only their own synthetic records. The actual Phase 5 verification uses a fresh `travel_agency_phase5_tests` database, separate temporary random administrator credentials, API 5081 and Vite 5174. Temporary QA configuration, logs, screenshots and capture files are outside Git under `/private/tmp/travel-phase5-*`.

### Admin production prerequisites

MFA enrollment/challenge/recovery UI and an approved credential recovery/rotation process are not implemented. Standard Identity TOTP support is the intended follow-up; MFA is required before production exposure. Password-only Phase 5 login refuses a two-factor-required account rather than bypassing it. This limitation is explicit and production readiness is not claimed.

Configure persistent restricted/encrypted data-protection keys, HTTPS and trusted proxy handling, approved hosts, same-origin hosting or reviewed exact credentialed CORS, appropriate frontend admin no-store/CSP headers, private database privileges, infrastructure-level rate limits for multiple instances, backup/restore, session policy and security monitoring before launch. API CSP currently denies document rendering and framing; the SPA needs its own deployment-server policy compatible with bundled styles/assets. Review cookie Secure/SameSite behavior in the actual HTTPS topology. Do not trust arbitrary forwarded headers or weaken cookies to fix deployment mistakes.

Default request/SQL URL logging is suppressed to avoid recording customer search terms. Security events use trace/actor IDs, not passwords, email addresses or note content. Request retention cascades notes and history; administrator accounts, captures and backups require explicit retention/access policies. There is no admin analytics integration, CSV export, quotation, invoicing, payment, booking or messaging workflow in this phase.

## Phase 5 verification — 2026-11-01

- Working branch: `phase-5-admin-dashboard`, created from merged Phase 4 main after clean-tree inspection and safe removal of the merged branch locally/remotely. No Phase 5 commit, push or merge was performed.
- Frontend formatting, ESLint, strict type checking, 102 tests across 13 files and production build passed. Tests cover protected routing, generic login errors/password visibility, logout, real-data rendering, filters/pagination, conflict handling, notes, localization, errors, safe return paths and stale-session-response invalidation. No frontend dependency was added.
- Backend locked restore, formatting and Release build passed with warnings treated as errors. The PostgreSQL-backed suite verifies authorization on all data routes, public submission, cookie flags, expiry with an intervening request, logout/security-stamp revocation, generic failures, lockout/IP throttling, CSRF, query pagination/search/filter/sort, safe detail/404, transitions/concurrent conflicts, notes/author isolation, activity, no-overwrite provisioning and rejection of password-only TOTP bypass. Unknown JSON fields and malformed query dates now return safe 400 responses rather than falling through to generic 500 handling. All 35 tests passed with none skipped, including production-mode Secure/HttpOnly/Strict/host-only cookie assertions.
- Migration applied to the existing development database and a fresh isolated `travel_agency_phase5_tests` database. All eight existing development enquiries retained distinct references, Submitted status and unchanged timestamps; each gained a nonempty concurrency version and original-time submitted activity. No administrator was seeded into the development database. EF reported no model drift; no database reset or previous migration rewrite occurred.
- Browser plugin was unavailable, so the existing Playwright Chromium/axe runtimes were used without installing dependencies. The real flow verified protected-route redirect/safe return, generic invalid login, original provisioned password after a no-overwrite provisioning retry, HttpOnly/Strict cookies, database dashboard counts, search/filter, complete detail rendering, escaped free text, status changes, persistent private notes, Swedish/English switching, reload, logout and actual one-minute cookie expiry. The existing public wizard successfully submitted another enquiry anonymously.
- Five axe scans (Swedish login, English desktop dashboard/detail, Swedish mobile detail/list) reported zero violations and incomplete checks. Twenty-four admin page/language/viewport checks covered dashboard/list/detail at 375, 768, 1024 and 1440px, including meaningful content, document language, no framework overlay, no horizontal overflow, logo bounds and keyboard skip-to-main. Desktop/mobile screenshots were inspected; sidebar logo clipping, select names and heading hierarchy were corrected. Sidebar keyboard focus uses a white ring against navy. These are Chromium/automated/keyboard checks, not a formal screen-reader or cross-browser certification.
- Thirty-two existing public page/language/viewport regression checks passed, including FAQ interaction, language persistence and image loading. The final-code browser smoke verified login/protected list/detail/logout and live rejection of a forged note author with HTTP 400. No unexpected browser errors occurred; expected anonymous/invalid/expired 401 responses were tracked separately.
- Nine isolated browser enquiries produced eighteen processed local captures. No external email was sent. Checked ordinary QA API logs contained no tested passwords, administrator/customer emails or note contents. npm audit found zero vulnerabilities and NuGet reported no vulnerable direct/transitive packages, including the new pinned Identity EF dependency.
- Temporary browser evidence remains under `/private/tmp/travel-phase5-*`. Temporary QA services, random account credentials and synthetic test fixtures are removed after verification; the normal development API/frontend remain available. Production MFA, credential recovery, protected persistent data-protection keys and the documented launch prerequisites remain unverified/unimplemented.

## Quotation configuration and operation

Apply the additive migration with the existing repository-root command before starting the new API. The API does not auto-migrate or seed quotations. Normal development still uses Vite 5173 and API 5080; no new frontend dependency or secret variable is required for draft editing.

| Variable | Default / purpose |
| --- | --- |
| `Quotations__IssuanceEnabled` | false; explicit commercial issuance approval gate |
| `Quotations__ReferencePrefix` | QUO; uppercase 2–8 characters |
| `Quotations__PublicOrigin` | Required for issuance; approved frontend origin, no credentials/path/query/fragment; HTTPS outside loopback development |
| `Quotations__DefaultValidityDays` | 14; range 1–90 |
| `Quotations__AccessRetentionDays` | 30 days after validity deadline; range 1–90 |
| `Quotations__PublicPermitLimit` | 60 public review/response calls per IP per minute, per process |
| `Quotations__ApprovedTaxTreatments__0` (etc.) | Explicit allowlist: VatExclusive, VatIncluded, NoVat; empty initially |
| `Quotations__ApprovedTaxRatesBasisPoints__0` (etc.) | Owner/adviser-approved rates, integer 0–10000; empty initially; required for selected VAT modes |
| `Quotations__TermsVersion` | Approved version identifier, at most 100 characters |
| `Quotations__TermsSv`, `Quotations__TermsEn` | Owner-approved terms, each at most 6000 characters |
| `Quotations__AcceptanceTextSv`, `Quotations__AcceptanceTextEn` | Owner-approved customer acknowledgement, each at most 2000 characters |

Do not interpret the supported tax modes as advice about which VAT treatment applies. No rate or legal wording is supplied by default. Have the owner/qualified adviser approve VAT, fee scope, payment timing, consumer information/cancellation/withdrawal rules and acceptance wording before enabling issuance. Terms must explain the service fee versus supplier travel costs and what happens after acceptance; never invent a waiver or automatic authorization to start work. Configure actual approved strings through trusted server configuration (with correct shell/JSON escaping for multiline text), not Vite or a new business settings CMS. Set the enabled flag only after all required values are reviewed. Restart the API after changes.

`GET /api/admin/quotations/configuration` reports readiness and missing categories to authorized admins. Missing approvals leave drafts usable and send disabled; the server returns 412 for a send that does not satisfy approval, including a draft with different legal terms. The draft editor can load the configured terms for that customer's language. Issuance freezes those terms/version and acknowledgement; later configuration edits do not update historical quotations. Production also needs the approved sender/domain and provider setup from Phase 4 and the MFA/key-management prerequisites from Phase 5.

### Administrator workflow

Open a non-Closed enquiry and select Create quotation draft. Each request gets one numbered series; if it already exists, use its current eligible version and Create revision instead of creating a second series. Enter the service description, included/excluded scope, 1–20 line items, quantities 1–100, two-decimal unit amounts, tax treatment/rates, future validity instant, approved terms and optional customer-facing notes. Use a dot as the input decimal separator. SEK/EUR/USD are supported; other currency precisions need an explicit future change. Amounts are bounded at 100,000,000 currency units per total; zero is allowed in a draft but not an issued offer.

Save before previewing/issuing. The local calculation preview is an estimate; the saved preview contains authoritative backend totals. A validity field uses the browser's stated time zone; API/storage use UTC, and customer views localize the timestamp. Send requires explicit confirmation and freezes the saved draft, mints the scoped credential and queues email in the same transaction. Quotation Sent means issued, not delivered. A lost response must be resolved by reloading current state; do not blindly create a new version.

Sent versions are immutable. Revise immediately revokes the old link and creates version N+1 under the same number; the old offer cannot be accepted while editing the replacement. Only the latest Sent/Declined/Expired/Cancelled version can be revised. Accepted is terminal. Draft/Sent can be withdrawn, and withdrawal revokes access; cancelled replacements never reactivate earlier versions. Status/concurrency conflicts require refreshing and reviewing. These actions do not change the underlying enquiry status or notify customers of a booking.

`/admin/quotations` supports server-side status filtering, search (quotation/request number, customer snapshot name/email), deterministic sorting and page sizes up to 100. Filters remain in memory rather than URLs/storage. Request detail includes paginated quotation versions; quotation detail includes immutable history across the series. Internal notes are never customer-facing.

### Customer links and response verification

Issued email contains a `/quote#credential` link. This intentionally differs from a token-in-path URL: the fragment never reaches HTTP access logs. The browser uses `X-Quotation-Token` with fixed public routes, keeps the credential only in memory/the provided fragment and never stores it in local/session storage. Avoid third-party resources, analytics, URL recording and header/body logging on this flow. The HTML has no-referrer metadata, and public API responses have no-store/no-referrer headers. Configure the frontend deployment server's sensitive `/quote` route with equivalent no-store, no-referrer and compatible CSP/anti-framing policy; don't rely on SPA metadata as a server header substitute.

The link authorizes only one version. Revise/cancel revoke it; access expires at the validity instant plus the configured read window. Within that window, expired/accepted/declined snapshots remain readable, but only Sent before its deadline can receive a new decision. Acceptance requires an initially unchecked checkbox containing the frozen approved acknowledgement. A confirmed response must come from the API before the UI shows success. Same-decision retries return the recorded result without duplicate activity/email; an opposite decision conflicts. No payment link, paid status or automatic work activation exists.

For raw API verification, admin calls use normal cookies and fresh X-CSRF-TOKEN. Public review is `GET /api/public/quotations` with X-Quotation-Token; responses are `POST /api/public/quotations/accept` or `/decline` with that header and `{ "confirmed": true }`. Never paste a real credential into a command URL or shared logs. Invalid/revoked/expired credentials return generic 404. Numbers/UUIDs alone cannot retrieve customer quotations.

The worker expires up to twenty due Sent quotations each polling cycle, with synchronous deadline checks on reads/decisions as a fallback. Test the boundary at the exact deadline and verify expiry activity appears once. Role authorization, CSRF, revision-versus-acceptance races, same-response replay and transactional rollback are covered by the PostgreSQL tests. The existing `_tests` database naming guard remains mandatory. The Phase 6 suite uses `travel_agency_phase6_tests`; do not reset the development database.

### Notifications, secrets and retention

Reused capture files under the API content root `.local-mail` include private review links and are ignored by Git; treat them like credentials. Local Capture sends no external emails. Quotation delivery uses the same bounded retries/dead letters and stable provider idempotency keys as Phase 4. The raw link is encrypted in the pending outbox with a separate Data Protection purpose; only its hash is used for customer authentication. Preserve/encrypt/restrict the shared production key ring or pending encrypted links cannot be delivered after restart. Successful/suppressed delivery clears the envelope; maintenance also clears envelopes for expired access tokens. A cancelled/superseded/expired issued message may be suppressed into dead-letter state rather than marked sent. Read the linked quotation state before manually requeuing such a job.

Customer accept/decline and agent notifications are distinct jobs. An email outage must not revert an issued/accepted/declined state. Actual provider/inbox delivery still requires the Phase 4 external configuration and recipient verification; local capture and fake HTTP tests do not demonstrate it. Do not include the complete enquiry or internal notes in these emails.

Quotation-linked requests are excluded from the older automatic request purger, even when that purger is enabled. The owner must approve commercial-record/acceptance retention, privacy notices, access/deletion procedures, capture-file/backups/provider policies and any later payment retention before automatic quotation deletion is introduced. Technical safeguards do not establish GDPR or consumer-law compliance.

## Phase 6 verification — 2026-11-01

- Work is on `codex/phase-6-quotations-customer-approval`, created from the merged Phase 5 main. Existing work was inspected and preserved. No Phase 6 commit, push or merge was performed.
- Frontend Prettier, ESLint, strict type checking, 119 tests across 14 files and production build passed. Tests include decimal parsing/per-line tax rounding, draft payloads without trusted totals, issuance gates/confirmation, escaped customer content, unchecked acceptance, duplicate-click guards, uncertain-response replay, English decline, expiry/unavailable states and confirmation focus, alongside all existing flows.
- Backend locked restore, formatting and Release build passed with zero warnings/errors. All 58 tests passed with none skipped using isolated PostgreSQL. Coverage includes money/lifecycle/version invariants, API authorization and CSRF, business configuration gates, immutable issuance, hash-only token lookup/protected envelopes, token revocation/expiry, decision replay/opposite-response rejection, concurrent acceptance/revision, background-only and synchronous expiry, safe notifications, failure/suppression, server-side quotation pagination/filtering/search and send-transaction rollback. Existing Phase 0–5 tests remained operational.
- The additive `20261101105446_Quotations` migration applied to a fresh `travel_agency_phase6_tests` database and the existing development schema. Before/after fingerprints of complete request/admin rows and existing outbox/activity counts matched. No database reset or previous migration rewrite occurred. EF reported no model drift. The scaffold warning was reviewed: Up widens the outbox kind and replaces its index while adding new tables/nullable associations; it does not delete existing records.
- Browser plugin was unavailable; existing Playwright Chromium/axe runtimes were used without adding dependencies. The real UI verified admin creation from a request, draft editing and authoritative totals, rejection of unapproved terms, configured terms, issuance/capture, Swedish customer acceptance/replay, English decline, revision immutability, immediate old-link revocation, cancellation, actual deadline expiry, series history, list search and next/previous server pagination.
- Twenty-four quotation page/viewport checks covered 375, 768, 1024 and 1440px across admin overview/editor/detail and accepted/declined/expired customer snapshots. Seven axe scans reported zero violations and incomplete checks. Desktop/mobile screenshots were inspected. Browser QA corrected textarea accessible names and moved focus/scroll to the confirmed customer heading; the final live check verified focus, visible confirmation and accepted-version finality. This is Chromium/automated/keyboard QA, not screen-reader or cross-browser certification.
- Thirty-two existing public page/language/viewport regression checks passed with no unexpected errors. A complete anonymous combined-service wizard submission also passed against the final Phase 6 API. Request status remained Submitted during quotation acceptance/decline/revision/cancellation; no payment, research activation or booking was created.
- The browser fixtures comprised eleven enquiries and eleven quotation-version rows. All 34 associated notifications were processed to safe local capture, with matching references, HTML/plain text and owner-only permissions. Sent envelopes were cleared. Customer tokens did not appear in network request URLs, browser local/session storage or checked ordinary API logs. Expected 401/404/412 test responses were separated from unexpected errors; the final runs had no unexpected page/resource/console errors. Real provider/inbox delivery was not tested.
- npm audit reported zero vulnerabilities; NuGet reported no vulnerable direct/transitive packages. No package versions/lockfiles changed; Infrastructure gained only the ASP.NET shared-framework reference needed for Data Protection.
- Test-only legal/tax strings were explicitly labeled fixtures, supplied through temporary private environment files, and never enabled in normal development configuration. Temporary QA processes/account/enquiries/quotations and random provisioning credentials are removed after verification; evidence/capture files under `/private/tmp/travel-phase6-*` contain only synthetic data. The normal API/frontend remain available with issuance disabled by default.
- Production still requires approved VAT treatment/rates, bilingual legal terms/acknowledgements/public origin, MFA/recovery/key protection, privacy/commercial retention and HTTPS/deployment review. Phase 7 invoicing, payment and activation remain unimplemented.

## Phase 7 invoicing and service payment operation

Work takes place on `phase-7-invoicing-service-payments` from main `7553f64`; never implement on main. Existing Phase 6 and later service styling changes were preserved. No automatic commit/push/PR is authorized.

Defaults are `Payments__Provider=Disabled`, `Payments__IssuanceEnabled=false`, `Payments__AccountingApproved=false`. Draft invoice creation from an accepted quotation works; issuance/payment collection is blocked until configuration is complete. No real payment provider is approved/integrated.

For **isolated Development fixtures only**, configure `Payments__Provider=Sandbox`, a private random `Payments__SandboxSigningSecret` of at least 32 characters, an explicit loopback frontend `Payments__PublicOrigin`, test seller name/address/registration/tax text, test `Payments__TermsSv`, `Payments__TermsEn`, `Payments__TermsVersion`, and explicit `Payments__IssuanceEnabled=true` / `Payments__AccountingApproved=true`. Use unmistakable TEST text; these switches do not constitute business/accounting approval. Approved quotation tax treatments/rates are reused. Existing quotation issuance/acceptance gates still apply. Sandbox is rejected outside Development. Never put signing keys in VITE variables or commit actual secrets. The checked-in example remains disabled; actual business configuration is not enabled by implementation.

`Payments__AccessDays` defaults to 90, range 1–366; scope tokens expire that many days after issuance. Due dates are explicit UTC timestamps (the editor sends its chosen date at 21:59:59 UTC); customers see dates in Europe/Stockholm. The local simulator expires sessions after 30 minutes. Long-due invoices may require an approved access-renewal workflow; no unbounded links or automatic reissue is provided.

### End-to-end development workflow

1. Apply migrations using the existing `scripts/with-env.sh dotnet ef database update --project services/api/TravelAgency.Infrastructure` command; never reset existing data.
2. Provision/login with an explicitly created administrator. Open an accepted quotation and create its single primary invoice draft.
3. Supply billing address, service period/agreed delivery and due date. Review frozen quote amounts and the saved preview. Issue with explicit confirmation only when the configuration gate is ready. Missing details reject issuance; no number/outbox/access token commits.
4. Development Capture email includes a private `/invoice#credential` link and TEST markers. The page calls fixed public API routes through `X-Invoice-Token`. Documents download as escaped HTML; print/save PDF in the browser if needed.
5. Open the labelled simulated checkout. Choose a simulated success, failure or cancellation; never enter card information. Follow the result link. Results fetch server state; `?success=true` cannot fabricate payment success. Failed/cancelled attempts can start another session. Pending returns show pending, with up to twelve checks every five seconds and manual refresh.
6. Inspect admin invoice detail/payment attempts, separated currency/simulation dashboard totals and request activity. Simulated success creates test-only eligibility, never real eligibility. No actual research or supplier booking begins.
7. Revoke an issued invoice link through the administrator confirmation when necessary. Revocation stops document/review/new-session access but does not undo existing provider evidence or block a genuine verified late settlement.

### Webhooks, reconciliation and operational review

The simulator's development protocol signs UTF-8 `timestamp.rawBody` with HMAC-SHA256 and expects hexadecimal `X-Payment-Signature` / Unix `X-Payment-Timestamp`; the five-minute tolerance and fixed merchant context are verified before application processing. This is simulator infrastructure only, **not** a claimed official provider integration. Test helpers generate valid evidence without exposing signing secrets to browsers. The webhook endpoint is `/api/payments/webhooks/Sandbox`; no browser-return endpoint marks invoices paid.

The simulator POST accepts `deliverWebhook:false` for controlled API tests: provider evidence changes, the app remains pending, and the existing worker reconciles from persisted provider evidence. Real reconciliation remains unavailable without a live adapter. Pending attempts are claimed in batches of ten, at most eight attempts with exponential minute delays capped at six hours. Exhausted attempts/discrepancies need operator investigation; there is no unaudited mark-paid override. The same worker marks batches of twenty overdue invoices; no fees or reminders are enabled. It clears expired simulator credential envelopes and expired/revoked invoice delivery envelopes. Keep the existing email worker enabled to run these jobs.

Email kinds: invoice-issued (also contains payment link), payment-confirmed, payment-failed and payment-agent. The existing outbox sender/retries/owner-only capture files apply. A pending failure message can describe its failed attempt even if another attempt later succeeds; it directs the customer to current invoice state. Queue/capture/provider acceptance do not prove inbox receipt. Check delivery failures and reconciliation discrepancies separately; email failure never rolls back a verified payment.

Use `GET /api/admin/payments/summary` and the dashboard for real records grouped by currency and simulation mode. Invoice listing uses 20-row SQL pagination with number search/status filter. Detail returns the latest 100 payment attempts; invoice/outbox/activity/evidence data remain in PostgreSQL. Commercial invoices/related requests cannot be automatically purged. Financial retention, backups, access expiry/renewal, incident handling and refunds/corrections require approved operations before production.

### Production blockers and Phase 8 boundary

Before enabling commercial issuance, obtain business registration/address/VAT or applicable exemption details; review invoice numbering/mandatory fields/service advance invoicing, tax treatment, consumer withdrawal/cancellation/refund rules, accounting retention, travel agency/package travel obligations and owner-approved bilingual terms. No legal identifiers, VAT rate/exemption, banking data or legal wording were supplied or invented.

Select and approve the payment provider/merchant arrangement, supported currencies/methods, hosted checkout, official webhook verification, refunds and provider-side reconciliation. Add the live adapter and official sandbox tests; only then consider production credentials. The current simulator is neither real payment processing nor a provider's official sandbox. Retain Phase 5/6 security prerequisites (MFA/recovery, HTTPS/proxy configuration, protected shared Data Protection keys, private SPA headers, no private-page analytics, privacy/security review). Phase 8 can consume only `ResearchEligible=true`, never `SandboxResearchEligible`. No research/proposal/booking/portal features are implemented here.

## Phase 7 verification — 2026-11-01

- Branch: `phase-7-invoicing-service-payments`, from main `7553f64`. Existing work preserved; no staging, commit, push or PR performed.
- Backend: locked restore, formatting verification and Release build with warnings treated as errors passed. **75 tests passed, zero failed/skipped** using isolated PostgreSQL. Coverage includes accepted-only invoice creation, exact immutable snapshots, lifecycle/number uniqueness and issuance rollback, auth/CSRF, blocked accounting configuration, scoped hash/envelope access, expiry/revocation, authorized escaped documents, session reuse, valid/invalid/stale signatures, merchant/reference/amount/currency checks, duplicate/concurrent/out-of-order events, atomic settlement rollback, pending/failure/cancellation/retry, provider outage, missed-webhook reconciliation, overdue settlement, production rejection of Sandbox, real database summaries/filtering and Swedish/English notification escaping/outbox retry. Existing Phase 0–6 tests remain included.
- Frontend: Prettier, lint, strict type checking, **133 tests across 15 files**, and production build passed. Payment-page chunks load on demand. Tests cover snapshots, escaping, simulation disclosure, loading/pending/backend-confirmed/failed states, duplicate-click guards, safe retries, invalid tokens, language switching, separated currency/simulation metrics, pagination and redirect spoofing. No dependency versions/lockfiles were changed.
- Migration `20261101221400_InvoicingServicePayments` applied to a fresh isolated QA database and upgraded the existing test/development schemas. EF reported no pending model changes. Complete existing request/quotation/admin row fingerprints and notification/activity counts matched before/after the development upgrade: eight requests, zero administrators, zero quotations, 16 notifications and eight activities preserved. No reset was used.
- Browser plugin was unavailable; existing Playwright Chromium/axe runtimes were used without adding dependencies. Actual UI exercised quotation acceptance, invoice creation/edit/issuance with explicit confirmation, Capture email link, protected HTML download, simulated success/failure/cancellation, retry sessions, backend-driven pending/result state, spoofed success query rejection, Swedish/English switching, real filter/summary results, link revocation with keyboard confirmation, retained payment history and continued authorized admin document access. The final API also accepted a complete anonymous combined-service wizard submission.
- **32 payment-page/viewport checks** covered 375, 768, 1024 and 1440px across admin list/detail/dashboard and customer invoice/result, including final tax-treatment labels. No horizontal overflow was found. **11 axe scans had zero violations**; actual Tab focus and Enter confirmation were verified. This is Chromium/automated and limited keyboard evidence, not screen-reader, cross-browser or complete WCAG certification.
- The existing public-page browser regression passed **32 route/language/viewport checks** at 320 and 1440px, including language persistence, FAQ, images, wizard route and not-found handling. No unexpected browser page/console errors or credentials in request URLs/API logs were found. Private API no-store, no-referrer, generic noindex metadata/no canonical and no browser credential storage were checked.
- **23 local email captures** were verified against successful outbox jobs, with owner-only file permissions and cleared delivery envelopes. One simulated eligibility record and zero real eligibility records were created. **No external inbox delivery, real charge, live provider or official provider sandbox was verified.** Current npm/NuGet audits reported no vulnerable packages; no whole-host/runtime security certification is implied.
- Temporary Phase 7 QA services, synthetic invoice/request/admin records and random credential/configuration files were removed after verification; evidence/screenshots remain outside Git. The normal development API and frontend remain available. Commercial payment issuance remains disabled; actual business/legal/provider prerequisites above are unresolved. Phase 8 was not started.

## Phase 8 research and proposal operation

Branch: `phase-8-travel-research-proposals`, from merged main `27bc6f9` (Phase 7 PR #9). The merged Phase 7 branch was verified against its PR head/merge before local and remote removal. Existing work was preserved. No automatic Phase 8 commit/push/merge is authorized.

### Configuration and eligibility

Apply the additive migration with `./scripts/with-env.sh dotnet ef database update --project services/api/TravelAgency.Infrastructure`; do not reset data. The Phase 8 schema adds research/workspace/proposal tables and nullable activity/outbox associations while keeping existing financial records intact.

Research requires **real** Phase 7 eligibility backed by a Paid non-simulated invoice, accepted quotation and matching Succeeded provider evidence for the same request. Sandbox eligibility and changing the enquiry's administrative status cannot unlock research. Closed requests cannot start or mutate paid research. There is no admin payment override, exception endpoint or `AllowSandboxResearch` flag. Phase 7's live-provider approval/integration is still outstanding; ordinary development data remains honestly ineligible. Tests may seed synthetic evidence only in isolated databases ending in `_tests`; never present that as a real payment or seed the business database with fake eligibility.

Proposal drafts use `Proposals__DefaultValidityDays` (default 14, range 1–90). Sending defaults off. To enable approved sending, configure `Proposals__SendingEnabled=true`, `Proposals__PublicOrigin` (HTTPS; explicit loopback HTTP in Development only), `Proposals__TermsVersion`, `Proposals__TermsSv`, `Proposals__TermsEn`, `Proposals__AcceptanceTextSv` and `Proposals__AcceptanceTextEn`. The owner must supply reviewed wording explaining supplier estimates, price changes, limits of approval and any additional confirmation required before booking. Exact approved language-specific terms must match the draft. No unlimited spending authority or legal wording is invented. `Proposals__AccessRetentionDays` defaults to 30 (range 1–90) after proposal validity; it extends read access, never the approval deadline. Never put credentials or private configuration in VITE variables.

### Administrator workflow

1. Open request details and follow Research workspace (`/admin/requests/:id/research`). The existing requirements/contact/budget/dates/travelers remain visible alongside the work area. Start research only when backend eligibility is satisfied.
2. Add a Flight, Accommodation or Extra option. Supplier data is manual: title/customer description, source HTTPS URL/name, conditions, price/currency/basis/explicit quantity, checked timestamps and typed details. Availability timestamp can stay blank and is displayed as not checked. No supplier website is fetched/verified and no inventory is reserved. Never enter supplier account credentials or private authenticated links.
3. Flight legs require ISO timestamps with explicit UTC offsets and matching timezone IDs (e.g. Europe/Stockholm). Chronology is validated. Journey numbers group connecting legs; use a new number for the return journey so intervening stay time is excluded from duration/connection counts. Store a supplier's priced itinerary honestly; do not invent flight availability or arbitrary price allocations.
4. Accommodation requires check-in/out, guests, destination/address, type/room, meals/amenities and cancellation terms. Extras capture category/place and optional date. Monetary support remains SEK/EUR/USD with two-decimal minor units. Explicit unit quantity is not automatically multiplied by guest/night counts; Whole-option pricing requires one unit. Unsupported currencies must not be converted silently.
5. Explicitly save each option; success is shown only after backend confirmation. Compare up to four same-kind options. Price currencies remain separate. Workspace notes and option notes are private and never included in customer snapshots. Notes save explicitly; no autosave is claimed.
6. Create the request's proposal series. Edit title, introduction, summary, validity, reviewed terms and up to six alternatives with one explicit recommendation. Each alternative includes 1–20 selected research items, with up/down item/alternative ordering. Save to refresh the selected snapshots. A changed research source after saving blocks send until you save/review again. Referenced source items cannot be removed; detach them from drafts first, and preserve historical associations.
7. Preview uses the same customer components and **last saved** server data. It includes travel requirements, source conditions/check times, separate supplier estimates per currency, and the already-paid service fee outside supplier totals. No inventory/price guarantee is made.
8. Send with explicit confirmation only when configuration and completeness pass. The transaction freezes this version, creates a version-scoped customer token and queues email. Queued is not delivered. Sent versions cannot be edited: revise the current Sent/Rejected/Expired/Cancelled version into a new Draft. Old links are immediately revoked. Approved is final in Phase 8. Explicitly revoking an issued customer link is still available and preserves the approved snapshot/decision.
9. Admin proposal lists filter/search by status/reference with 20-version SQL pagination. Details/preview/history show selected alternative and customer response; rejection feedback is private to administrators. Activity history is paginated at 50 events. Request details link the workspace and show proposal version history/approval/selected alternative without duplicating customer records.

### Customer workflow and security

The email opens `/travel-proposal#credential`. Fixed `/api/public/travel-proposals` GET/approve/reject endpoints use `X-Proposal-Token`; raw credentials never appear in HTTP URL paths/query strings or browser local/session storage. Do not add analytics, external embeds or public static proposal files. Preserve production HTTPS/private SPA no-store/referrer/CSP and secure shared Data Protection key persistence from Phases 5–7. Email/browser history still contains private bearer links; retain owner-only email-capture permissions and approved retention.

A customer reviews alternatives and estimated costs, selects one radio (nothing preselected), checks the frozen acknowledgement (unchecked by default), reviews the selected alternative/currency totals and explicitly confirms. The backend checks version, terms, selected membership, deadline, status and eligibility; replay of the same choice is idempotent, different/opposite decisions conflict. Rejection has explicit confirmation and optional feedback. Network failures do not show fake success; refresh reads the saved state. No decision books suppliers, collects supplier payments or automatically refunds service fees.

The existing notification worker processes bounded proposal expiry (20 due versions/poll), clears expired/revoked delivery envelopes and reuses the existing outbox for five localized mail kinds. Same-series mutation/decision locks and optimistic tokens protect approval/revision races. Customer responses never expose internal research notes, customer email, request/source IDs or history. No extra job platform, email sender, auth service, file storage or state framework was introduced.

### Phase 9 boundary and remaining prerequisites

A future booking workflow may consume only the immutable Approved proposal/version, selected alternative and recorded terms/acknowledgement. Supplier prices/inventory must be rechecked; price changes or extra authority may require new explicit customer confirmation. Phase 8 does not implement supplier execution, purchases, booking references, supplier payment collection, refunds, travel document generation, customer portal/messaging or business settings CMS. Real payment-provider integration, commercial legal/tax/travel rules, approved proposal wording, private hosting/key management, security review and retention procedures remain launch prerequisites.

## Phase 8 verification — 2026-11-02

- Work remains on `phase-8-travel-research-proposals`, from main `27bc6f9`. The merged Phase 7 branch was removed locally/remotely after verification. No Phase 8 staging, commit, push or PR was performed.
- Backend locked restore, formatting verification and Release build with warnings as errors passed. **97 tests passed, zero failed/skipped**, using dedicated `travel_agency_phase8_tests` PostgreSQL. Coverage includes unpaid/simulated denial, real-evidence association, auth/CSRF, research CRUD/notes/conflicts, safe supplier URLs/timezones/price quantities, separated currency totals, explicit version/terms/alternative approval, replay/opposite-choice rejection, concurrent approval/revision, cross-request isolation, immutable sent snapshots, revision/cancellation/hash expiry, background-only and synchronous expiry, sending gates/rollback, private DTOs, approved-link revocation, SQL list filtering and actual public-endpoint rate limiting/Retry-After. Swedish/English proposal notifications and bounded outbox retry were exercised alongside existing Phase 0–7 tests.
- The first reused integration database had an older QA worker attached and an email assertion raced its outbox consumption. Final results use a fresh dedicated database with no running API worker attached. Never share a backend integration-test database with a live QA API/notification worker.
- Frontend Prettier, lint, strict type checking, **151 tests across 16 files**, and production build passed. Tests cover editor payloads, exact minor units, multi-leg controls/journey durations, comparison, alternatives/recommendations/ordering, saved preview, unchecked approval/selection, confirmation/focus, rejection/feedback, uncertain-response retry, duplicate-click guards, escaping/private notes and language switching. Private metadata remains noindex/no canonical, including trailing-slash routes with a configured public origin. A browser-discovered duplicate React key was fixed with distinct editor/action keys and a regression test.
- Migration `20261101231130_TravelResearchProposals` applied to fresh QA/test databases and the existing development schema. EF reported no pending model changes. Complete original-column fingerprints matched before/after upgrade: eight requests, zero administrators/quotations/invoices/payments, 16 outbox records and eight activities preserved. No reset or earlier migration rewrite occurred. Generated EF metadata remains tool-managed; handwritten feature components/services stay focused and small.
- Browser plugin was unavailable; the existing Playwright Chromium/axe runtimes were used without installing dependencies. Actual UI checks exercised denied unpaid/simulated research, request-detail navigation, research start, flight/stay/extra entry, comparison, private notes, option edit/removal, proposal alternatives/recommendation, save/preview/send, private Capture link, explicit Swedish approval, English rejection/feedback, immutable approval, revision/revocation/cancellation, actual deadline expiry, backend filter results and saved mobile revision. Approved-link revocation preserved the complete customer snapshot/decision and returned 404 for the revoked link. A complete anonymous combined-service wizard also submitted successfully against the final QA API.
- **32 research/proposal viewport checks** covered 375, 768, 1024 and 1440px, including editable extra/proposal forms. No horizontal overflow was found. **12 automated axe scans had zero violations**, including mobile editing and final readable flight-time preview. A heading-level jump on the proposal results page was corrected. Actual Tab/Enter confirmation and decision-heading focus were verified. This is Chromium/automated/limited keyboard evidence, not full screen-reader, cross-browser or WCAG certification.
- Existing public-site regression passed **32 route/language/viewport checks** at 320/1440px, including FAQ, language persistence, images, wizard and not-found routes. Final successful browser runs had no unexpected console/page errors, token leaks in HTTP URLs/API logs or automatic supplier fetches. Customer-side labels are readable; frozen local flight times/offsets remain unchanged even when browser timezone databases differ.
- **10 associated local email captures** matched successful current QA outbox jobs, had owner-only file permissions, excluded private research notes and left no delivery envelopes. Proposal-sent, customer decision and agent decision notifications were exercised in Swedish/English. No external inbox delivery, actual supplier availability or real payment/booking was verified. Paid QA eligibility was explicitly synthetic and isolated.
- Current npm/NuGet audits reported no vulnerable packages. Dependency versions and lockfiles were unchanged. Production legal/payment-provider/terms/key-management/private-hosting prerequisites remain unresolved; research cannot use simulated eligibility and sending stays disabled by default.
- Temporary Phase 8 QA services, synthetic requests/research/proposals/invoice evidence/admin account and random credentials/configuration were removed after verification. Evidence/screenshots remain outside Git. The normal development frontend/API remain available. Phase 9 was not started.

## Phase 9 — Booking management operation

Branch `phase-9-booking-management` starts from default `main` at `8c528b4` after verified merge of Phase 8 PR #10. The matching Phase 8 branch was removed locally and on GitHub. No Phase 9 staging, commit, push or merge is performed automatically.

### Setup and eligibility

Apply `20261102155928_BookingManagement` through the existing command:

```bash
./scripts/with-env.sh dotnet ef database update --project services/api/TravelAgency.Infrastructure
./scripts/with-env.sh dotnet ef migrations has-pending-model-changes --project services/api/TravelAgency.Infrastructure
```

Do not reset the database. The migration adds booking/reservation/event tables, a booking-reference sequence and nullable activity/outbox associations with JSON mail payloads. Existing requests, financial records, research and proposals are retained. No new runtime environment variable or supplier API credential is needed.

The backend requires the same request's real non-simulated Paid invoice/accepted quotation/matching successful provider evidence, a current Approved proposal version, matching selected-alternative decision and frozen terms/acknowledgement. Closed requests, simulated eligibility and arbitrary request status edits do not authorize booking. Live payment-provider integration and approved proposal terms from Phases 7–8 remain commercial prerequisites. Do not insert fake eligibility into the business database.

### Administrator workflow

1. Open the approved proposal and choose **Create booking workflow**. Record the customer's explicit external booking authority: customer name, Email/Phone/SignedDocument/Other channel, reference/evidence, UTC approval time and unchecked acknowledgement. The evidence must cover the selected arrangements and follow proposal approval. This is an administrator-recorded assertion of externally collected customer authority; it does not impersonate a customer signature or create a portal.
2. The server creates a unique `BKG-year-sequence` reference and one required reservation per selected proposal item. These internal references are not airline/hotel references. `/admin/bookings/:id` displays requirements, selected alternative, original estimates, current supplier costs grouped by currency, checklist, private notes and history. `/admin/bookings` supports reference/customer search, status filtering, updated/departure/reference sorting and 20-item SQL pagination.
3. Edit the actual/proposed reservation through the shared typed flight/stay/extra fields. Flight legs retain explicit UTC offsets and time zone IDs, journey groups, cabin and baggage. Traveler count comes from the approved trip. Accommodation includes dates/guests/room/meals/cancellation conditions; extras include category/location/date. Record current supplier source, exact unit cost/quantity/currency, conditions, price check and availability check. The application never fetches supplier links or guarantees inventory.
4. Explicitly choose `CustomerPaysSupplier`, `AgentPaysSupplier` or `OtherApprovedArrangement`, its evidence/arrangement reference and supplier payment status. No payment responsibility is preselected. The owner must establish the legally approved operating model. These are tracking facts only; supplier costs are never charged, collected or counted as service-fee revenue by this feature.
5. Start the manual reservation only after backend eligibility, payment responsibility and price/availability checks within 24 hours pass. Perform the real supplier operation externally. Record **Awaiting confirmation** if necessary; neither action claims that a supplier has confirmed anything.
6. Higher cost, changed currency, supplier/flight/room/dates/conditions/unit quantity or changed recorded payment authority triggers review. Save the proposed change, obtain genuine external customer reapproval, and record evidence against that exact arrangement version. The approval must follow the saved change. An unrelated later edit cannot clear outstanding review. Another changed arrangement invalidates prior reapproval. Lower cost alone with otherwise identical terms does not trigger initial review. No higher-price/replacement auto-approval exists.
7. Record supplier confirmation only after receiving genuine reference or alternative confirmation evidence, a valid UTC confirmation timestamp, and refreshed price/availability checks. Optional confirmation email/document references are stored privately; URLs must be safe HTTPS and are never fetched. Flight ticket issuance is tracked separately. A reference may remain blank when the supplier uses legitimate alternative evidence; evidence text is mandatory. Corrections can be recorded with current supplier evidence. No supplier reference is generated by the app.
8. If one reservation fails, record its reason; the agent notification is queued. Other confirmed reservations remain intact. Failed reservations can restart after current checks/authority pass. Editing an already confirmed arrangement resets its current confirmation and requires fresh supplier confirmation; immutable before-change events retain the earlier supplier facts. Cancellation records require explicit supplier cancellation evidence/time/reason and optional known charge in the current reservation currency. Cancellation is terminal for that reservation and never executes supplier cancellation or refunds. Historical supplier details are retained.
9. Overall status is derived, never manually selected. All required reservations must be Confirmed for the entire booking to be Confirmed. Any confirmed item with another incomplete/failed/cancelled item yields PartiallyConfirmed. All cancelled yields Cancelled. A failed/cancelled required item with none confirmed yields Failed; untouched items yield Pending, active items InProgress. This intentionally highlights trips that cannot currently be completed as approved.
10. **Queue full booking confirmation** requires explicit confirmation and all required supplier reservations confirmed. It queues a localized immutable snapshot once per booking revision; queueing is not delivery. Delivery status shows queued/retry, accepted by sender, or failed/discarded. The worker discards a full-confirmation job if the booking changed before sending and locks that aggregate during delivery. No full-trip template is sent for partial bookings. Further changes require reviewing and explicitly queueing the current state.

The dashboard uses real SQL counts for pending/in-progress trips, partial/full confirmations, failed supplier reservations and trips departing in 30 days. Trip metrics count aggregates, not reservations. Request details link the booking/progress/current costs. Audit pages paginate 50 events and retain before/after arrangements, initial authority, reapproval, actors and timestamps; detailed evidence remains admin-only.

### API contracts and security

All routes require the existing Administrator policy, secure cookie, CSRF header for mutations and private no-store responses. DTOs are returned; EF entities are not public contracts.

- `GET /api/admin/bookings?page=&status=&search=&sort=&requestId=` — bounded list; `sort=updated|departure|reference`.
- `GET /api/admin/bookings/metrics` — real aggregate/reservation metrics.
- `GET /api/admin/bookings/{id}` and `GET .../{id}/activity?page=` — private workspace/history.
- `POST /api/admin/proposals/{id}/bookings` — `{authorization:{customerName,channel,reference,approvedAt,confirmed}}`.
- `PATCH /api/admin/bookings/{id}` — `{versionToken,notes}`.
- `PATCH /api/admin/bookings/{id}/reservations/{rid}` — `{versionToken,option,paymentResponsibility,paymentStatus,paymentArrangement}`; `option` reuses Phase 8 typed manual supplier fields.
- `POST .../{rid}/start|await` — `{versionToken}`.
- `POST .../{rid}/reapprove` — `{versionToken,evidence:{customerName,channel,reference,approvedAt,confirmed}}`.
- `POST .../{rid}/confirm` — `{versionToken,supplierReference,evidence,documentReference,ticketingStatus,confirmedAt}`.
- `POST .../{rid}/fail` — `{versionToken,reason}`.
- `POST .../{rid}/cancel` — `{versionToken,reason,supplierEvidence,chargeMinorUnits,cancelledAt}`.
- `POST /api/admin/bookings/{id}/send-confirmation` — `{versionToken}`.

UTC fields support seconds/milliseconds. Supplier flight fields preserve timezone-aware local offsets. Mutations use the parent request and booking row locks plus optimistic version tokens; stale state returns 409, invalid fields/transitions 400, ineligible payment 403, missing/cross-booking IDs 404. Reload current data before retrying uncertain responses. No passport/card/supplier credential fields or unauthenticated booking-details endpoints exist. Same-category replacements use the existing selected item; unapproved add-ons and new categories must not be smuggled into an approved trip.

The service-fee workflow, supplier costs and booking state remain separate. No customer portal/accounts, itinerary/document upload/generation, supplier API execution, supplier payment collection, automatic flight/hotel purchase, messaging or settings CMS is implemented. Phase 10 requires explicit authorization.

### Phase 9 verification

- Backend locked restore, formatting verification, Release build with warnings treated as errors and **115 tests passed, zero failed/skipped**, using a fresh dedicated `travel_agency_phase9_final_tests` PostgreSQL database. Coverage includes Phases 0–8, admin/CSRF, current approval/payment association, simulated-payment denial, cross-booking IDs, duplicate creation, concurrent saves, transitions/status calculation, confirmed-item preservation, price/terms/payment reapproval, sticky review, currency separation, cancellation evidence, history, pagination/metrics, bilingual escaping, actual outbox delivery through a test sender and stale-confirmation discard. No live QA worker shared the integration-test database.
- Frontend formatting, lint, strict type checking, **160 tests across 17 files** and production build passed. Tests include authority/unchecked controls, failed saves, partial/full booking actions, exact minor-unit edits, payment responsibility, server-side list queries, bilingual rendering, and keyboard focus restoration. The shared travel-option editor's original research behavior remains covered. No application dependency or lockfile changed. axe-core 4.14.0 was used only as temporary QA tooling outside the repository.
- Migration `20261102155928_BookingManagement` applied to fresh test/QA databases and the existing development database. Seven total migrations and no EF model drift were verified. Original-column fingerprints matched for 12 existing development tables: eight requests, 16 outbox rows, eight activities and zero existing quotations/invoices/payments/proposals/admin users remained intact. The initial fingerprint query assumed an `Id` on the request-keyed eligibility table; that query was corrected separately. Eligibility had zero rows and the migration contains no mutation of that table. No reset or old migration rewrite occurred.
- Browser plugin was unavailable; existing Playwright Chromium plus temporary axe were used. The real rendered flow covered approved-proposal navigation, booking-authority entry, flight reservation editing, payment responsibility, actual-source checks, partial confirmation, higher-price hotel reapproval, transfer failure/restart, full confirmation, local capture, cancellation/charge tracking, search/filter/sort, request and dashboard integration. Synthetic provider/supplier evidence was confined to `travel_agency_phase9_final_qa_tests`; no real money, supplier purchase or supplier cancellation occurred.
- **40 booking layout checks** covered 375, 768, 1024 and 1440px, Swedish/English workspace/list/dashboard/request integration and accommodation/extra editors. **28 public-site route/language/viewport checks** covered homepage, services, pricing, about, contact, FAQ and wizard at 375/1440px. Page identity/nonblank headings, absent framework overlays, overflow and loaded images passed. Successful runs had no unexpected console/page errors or automatic supplier URL fetches.
- **14 successful automated axe scans had zero violations and no incomplete results.** Browser testing found the local text-button contrast issue on the notice surface and it was fixed within the booking module. Actual Tab/Enter focus into and out of confirmation controls, explicit actions and language persistence passed. Accessible select naming was made explicit. Screenshots were visually inspected on desktop/mobile. This is Chromium, automated and limited keyboard evidence, not screen-reader/cross-browser testing or full WCAG certification.
- **Three local email captures** covered Swedish and English customer confirmations plus the agent's supplier-failure notification. Captures were owner-only, preserved supplied TEST references, escaped HTML and excluded private research notes. Application sender acceptance was verified; external inbox delivery, real payment-provider processing, real availability and actual supplier reservations were not verified. The inherited at-least-once transport limitation remains.
- Commercial prerequisites remain: reviewed customer/supplier booking authority and operating/payment model, approved legal/tax/travel terms, actual payment provider, private production hosting/key management and privacy/security review. The application records manual external evidence; it cannot independently authenticate a supplier email or customer's phone approval. Cancelled reservations are terminal; a distinct unapproved item/category cannot be added to an already approved trip. Broader portal, document, itinerary and supplier integration workflows remain deferred.

- Temporary Phase 9 QA API/frontend processes, three synthetic approved-request fixtures, booking/reservation/history records and test administrator were removed. Guarded QA cleanup verified zero requests/bookings/reservations/events/administrators; random credential configuration was deleted. Screenshots, captured TEST emails and verification logs remain outside Git. The existing development database was preserved, the normal development API/frontend are available, and Phase 10 was not started. Changes remain uncommitted for review on the Phase 9 branch.
