# Development

## Phase branch workflow

Never implement changes directly on `main`. Before each phase, inspect Git status and preserve any existing work. When the previous phase is merged and the working tree is clean, switch to `main`, pull with `git pull --ff-only origin main`, then create the assigned phase branch before editing files. Phase 3 uses `codex/phase-3-travel-request-wizard`.

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

Open <http://localhost:5173>. The API listens on <http://localhost:5000>. Vite refuses a busy port instead of silently switching to an origin that CORS does not allow. Use `npm --prefix apps/web run preview` after a frontend build for a local build preview; configure its origin explicitly if testing API calls on another port.

The public homepage makes no health requests. Use `/_dev/health` on the development server for API diagnostics; that route is absent from the production build. Public pages are `/services`, `/pricing`, `/about`, `/contact` and `/faq`, in addition to `/`. `/about-preview` redirects to `/about`. `/plan-your-trip` opens the travel wizard; submission is disabled. Existing homepage anchors (including `/#sa-fungerar-det`) are preserved.

### Language and contact verification

Choose Svenska or English in the header, navigate to another page, then reload. All page copy, image alternatives, metadata and document language should follow the selection. The preference is local to the browser origin, under `travel-agency.language.v1`; clear that key to test the Swedish default. Invalid or inaccessible storage falls back safely. Keep shared translation keys and stable IDs in sync when editing `src/content/sv.ts`, `en.ts` and `public.ts`.

Leave `VITE_CONTACT_EMAIL` blank until a real mailbox is approved. To verify the configured contact flow locally, set a test mailbox in `apps/web/.env`, restart Vite, fill the required fields and prepare the draft. Confirm that an encoded mailto link appears and that no delivery confirmation is displayed. Open the link only when you intend to use a local email client; actual sending happens there. No message is posted to the API or saved by the website. Remove the test address afterward. Vite exposes these values in the browser, so never use provider API keys here.

`VITE_SITE_URL` is optional until the public domain is approved. Use an HTTPS origin to emit path-specific canonical links. Localized client metadata is implemented, but noindex/robots restrictions remain deliberate. Launch requires reviewing indexing, legal content, multilingual URL strategy and static-host SPA fallback routing (all public paths must serve index.html). No SSR, sitemap or hreflang claim is made in Phase 2.

```bash
curl -i http://localhost:5000/api/health
curl -i http://localhost:5000/api/health/ready
docker compose ps
docker compose logs --tail=50 postgres
```

## Travel wizard development and verification

Use the repository root for `npm --prefix apps/web ...` commands, or change into `apps/web` and run `npm ...` directly. The wizard requires no new environment variables, credentials, backend process or database.

Open `/plan-your-trip` for generic service selection, or use `?service=flights`, `accommodation`, `flight-accommodation` or `complete` to preselect a service. Unknown values are ignored. Homepage and Services-page cards use the shared mapping in `features/travel-request/config.ts`. The flow is configured by service, not by translated labels or hardcoded numeric step indexes.

For a manual journey, select a service; enter a departure location when flights apply; enter a destination or opt into suggestions; set fixed/flexible dates; enter travellers/child ages and rooms when applicable; set a positive budget; choose relevant preferences; add optional requests; enter contact details; and review. Try blank/invalid fields to confirm progression is blocked. Use Back to retain answers, or an Edit button in review and Back to review to validate all answers again. Changing service resets service-specific choices. Switch languages mid-step and confirm values and translated errors are preserved. The final send control must remain disabled, with no request traffic, success confirmation or reference number.

Draft saving is optional. Tick the save checkbox and inspect `travel-agency.request-draft.v1` in browser storage: it must contain only service, dates, traveller counts/ages, budget, structured preferences and an activity-help boolean. It must not contain contact, destination/origin/stops or special-request text. Reload to restore permitted answers, then re-enter omitted details. A stored draft restores at service selection rather than jumping over validation. Invalid/expired drafts are discarded on mount. Confirm that Start over clears this key and unchecks saving without changing the separate language preference. Blocked storage must produce feedback and still allow the journey in memory.

`model.ts` holds input and payload types; `reducer.ts` handles answer updates and service/child-count transitions; `validation.ts` is shared by step gating and payload construction; `payload.ts` is the Phase 4 integration boundary. UI components never post the payload or persist it as a draft. Extend the existing unit/interaction tests whenever a conditional rule changes. Future server validation must repeat all applicable rules independently.

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
