# Architecture

## Scope

Phase 0 creates a modular monolith with a separate React frontend and a single ASP.NET Core API backed by PostgreSQL. Phase 1 builds the public homepage; Phase 2 adds bilingual public information pages, Phase 3 adds a local travel request wizard, and Phase 4 connects it to an independently validated submission API, PostgreSQL and a durable notification outbox. Phase 5 adds Identity-backed administrator authentication and request management. There are no microservices, customer accounts or payment integrations.

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

`main.tsx` initializes React, BrowserRouter, self-hosted fonts and global styles. `app/App.tsx` places the localized error boundary inside LanguageProvider, and declares the homepage, five public pages, the travel wizard and confirmation route, a legacy about redirect and the not-found route within the public layout. The Phase 0 `FoundationPage` remains on the development-only `/_dev/health` route with its original health hook and success/failure/pending tests.

`app/config` owns branding and destinations; `content/sv.ts` and `content/en.ts` own typed homepage/shared copy; `content/public.ts` owns bilingual page resources; `content/images.ts` owns responsive media references. `components/ui` provides Button/ButtonLink, Container, Section, Card, and Icon; `components/layout` owns header, brand, mobile disclosure navigation, and footer. `features/home` composes separate section components, keeping the router and page composition small.

Global SCSS is limited to tokens, typography, resets, focus, and reduced-motion rules. SCSS Modules own component layouts and states. Shared SCSS breakpoints provide consistent service grids and navigation. Stateful behavior stays local: header disclosure state and route/anchor focus management. Language is the shared application preference; wizard answers remain local, with draft-storage availability observed externally. There is no state-management or animation framework.

Travel imagery is bundled locally, with responsive WebP and an AVIF hero plus fallback. The hero is prioritized/preloaded; below-the-fold images lazy-load with stable dimensions. Fonts and icons ship their license notices. See [the design system](DESIGN_SYSTEM.md) and [asset provenance](ASSETS.md).

`services/apiClient.ts` centralizes the Axios base URL, timeout, and safe error conversion. `services/health.ts` validates the runtime health payload instead of trusting a TypeScript cast. The hook cancels in-flight work on unmount and handles rejections. SCSS Modules own layout styling; global styles cover typography and basic document defaults.

`VITE_API_BASE_URL` is public configuration embedded at build time. If unset, Axios uses same-origin URLs, suitable for a future reverse proxy. Local development uses the explicit URL in `apps/web/.env`. There is no production API host hardcoded in components.

Swedish is the default language. `localization/LanguageProvider.tsx` initializes from a validated localStorage preference (`travel-agency.language.v1`, only `sv` or `en`), updates the document language and persists explicit choices. Storage errors are caught so switching still works in memory. `useTranslations` and `usePublicCopy` select centralized resources; English shared copy must satisfy the Swedish resource shape. Stable IDs, icon names and image keys are retained across languages. Language switching preserves the route and FAQ disclosure state; prices are not fabricated. No i18n dependency is required for two static languages.

## Public routes and page composition

| Route | Behavior |
| --- | --- |
| `/` | Existing homepage, fully localized |
| `/services` | Four services, scope, suitability and optional activity help |
| `/pricing` | Service comparison, quote model, separate service and travel costs |
| `/about` | Approach and experience, with explicit owner/portrait placeholders |
| `/contact` | Public contact configuration and honest draft-only form |
| `/faq` | Native details/summary disclosures, keyboard accessible |
| `/plan-your-trip` | Bilingual travel request wizard with explicit server submission |
| `/plan-your-trip/success` | Confirmed receipt only; direct visits without context show an honest missing-receipt state |
| `/about-preview` | Replace redirect to `/about` for existing links |
| other paths | Localized not-found content |
| `/_dev/health` | Existing diagnostics in development only |

`features/public` separates each page from shared PageIntro, styles, copy selection and contact draft construction. Existing Section, Container, Icon and Button components remain in use. Header uses NavLink for the current page; disclosure navigation switches below 1280px to leave room for both languages. RouteFocus preserves main/anchor focus and scroll behavior, including legacy homepage anchors.

The contact form accepts a public mailbox from build-time `VITE_CONTACT_EMAIL`, validates it, and disables all fields without one. Configured mode uses native constraints plus trimmed-value validation, then exposes an encoded mailto link. It does not automatically open an email client on form submission, send HTTP requests, save personal data or assert delivery. Editing clears the prepared draft. Actual delivery requires the visitor's configured email client and mailbox infrastructure, outside this phase.

Localized page titles, descriptions, Open Graph locale/content and Twitter summary metadata update on navigation and language changes. Canonical links require a configured HTTPS `VITE_SITE_URL` and use its origin plus the current path. The provisional site retains noindex/nofollow and disallowing robots.txt. The same URLs serve both languages, so no misleading hreflang alternate URLs are emitted. Approved domain, locale-specific indexing, prerendering/SSR, sitemap, company schema and legal launch content remain deferred. Payments and bookings remain deferred; Phase 4 enquiry persistence is described below.

## Travel request wizard (Phase 3)

`features/travel-request/WizardPage.tsx` owns the current step, review-edit mode, validation feedback and draft opt-in. A pure `wizardReducer` owns all form answers; values remain as input strings until payload construction, so empty/in-progress numeric inputs are preserved correctly. `model.ts` defines form data and the separate normalized `TravelRequest` contract. `config.ts` validates service identifiers, derives steps and maps the existing Swedish card IDs to stable service query values. The selected service stays synchronized with the query using replace navigation, so refreshing after a service change cannot reapply an old entry-link selection. Query-only updates retain form state and control focus; start over removes the service query. No new runtime dependency is added.

`steps` separates service selection, trip details, preference fields and personal details. `components/Fields` associates labels and translated errors with controls; `components/Review` renders every relevant answer and an edit action for each section. The existing layout, buttons, route focus and language provider are reused. Copy in `copy.ts` is shape-checked across Swedish/English; language changes keep the current step and answers.

### Dynamic flow and conditional answers

All journeys contain service → destination → dates → travellers → budget → relevant preferences → additional requests → contact → review. Flight and accommodation preferences are separate steps when both apply. Complete planning adds an itinerary/interest step, resulting in 9 steps for either single service, 10 for combined service and 11 for complete planning. Progress is derived from the selected service rather than hardcoded.

- Flights, combined and complete planning require an origin; accommodation alone does not. Flexible destination choice makes a specific destination optional. Only complete planning exposes additional destination stops.
- Fixed-date flight-only requests can be one-way. Accommodation journeys require an end after the start. Flexible mode captures an earliest/latest window and a duration that fits inside it; hidden fixed-date answers are omitted from the payload.
- Children reveal an age entry for each child; reducing the count removes extra ages. Rooms are required only when accommodation is included. Phone is required for phone contact, and any supplied optional phone must still be valid.
- Changing services keeps shared travel/contact answers, resets service-specific preferences, disables one-way mode and resets room count. Irrelevant origin/stops/room/preference fields never enter the final payload. Previous-step navigation keeps current in-memory answers. Review edits revalidate the complete request before returning to review.

### Validation and accessibility

`validation.ts` provides pure field-key/error-code maps for each step plus whole-request validation. It checks trimmed required text and lengths; real calendar dates, local today and a ten-year horizon; ordered date windows; integer duration, counts, ages and rooms; positive bounded budgets with at most two decimals; allowed choice values; email and conditional phone validity. Limits are stated in the UI. Step navigation is blocked on errors, and the payload builder repeats whole-request validation. Phase 4 independently validates untrusted requests server-side; these checks do not establish a server security boundary.

The current step has an accessible heading and aria-current progress. Advancing moves focus to the step heading; errors move focus to a linked summary and use aria-invalid/aria-describedby on fields. Native radio, select, checkbox and fieldset controls support keyboard operation. Start over uses an inline confirmation and restores focus on cancel. The mobile progress view shows the current step, while the full step list remains available on desktop. The confirmation route requires a confirmed receipt; it never invents one.

### Payload boundary

`buildTravelRequest(data, language)` returns `undefined` if any applicable step is invalid. Otherwise it returns an independent normalized `TravelRequest` object: schema version, language, discriminated service/preferences, trimmed destination/origin/optional stops, fixed or flexible dates, numeric adults/child ages/optional rooms, numeric budget/currency/scope, activity assistance, optional special requests and normalized contact details. An omitted return date represents a one-way fixed flight. No request ID, server status, timestamp claiming receipt, quotation or booking is manufactured. Review constructs this object in memory. Explicit send passes it to the API adapter; it is never serialized to browser draft storage.

### Permitted draft persistence

`draft.ts` explicitly projects a safe subset before writing localStorage. Storage is off by default and enabled only by the visitor. Contact details, all free text (including destination/origin/stops) and special requests are excluded even if extra keys are injected into stored JSON. Drafts use version 1 and a seven-day restoration window, reject malformed/oversized records, unknown enums/types, unsupported versions and implausible future timestamps, and reconstruct only known keys. Expiry is enforced on read; abandoned browser storage is not erased by a background service. The next wizard mount removes unusable drafts. Restoration starts at service selection and asks for excluded details again, so incomplete data cannot silently become a valid request.

A small `useSyncExternalStore` subscription observes storage-write availability. Blocked access/quota failures show an honest message while in-memory editing continues. Start over clears in-memory answers, disables opt-in and removes this draft key; if browser storage denies removal, the UI explicitly says it cannot remove the draft and advises clearing site data. Language preference is a separate key and is preserved. Local storage is not encrypted or cross-device storage.

### Phase 4 submission boundary

`services/travelRequests.ts` uses the existing Axios client and validates HTTP 201 and the runtime receipt. `useRequestSubmission` owns pending, validation, uncertain-network, server, rate-limit and conflict states; a synchronous in-flight guard prevents duplicate clicks. Navigation and editing are locked while pending. Server field codes map back to wizard steps. Retry-After controls retry availability; there is no automatic new submission after an error.

`submissionStorage.ts` persists only a UUID and timestamp under `travel-agency.submission-attempt.v1`. It reuses this key after uncertain failures, including remounts. Expired (seven-day), corrupt or unavailable attempt storage blocks sending rather than silently creating a new identity. Start over explicitly warns before discarding an attempt: doing so can create another enquiry if an earlier response was lost. Confirmation clears the matching attempt and permitted draft, with a warning if browser cleanup fails. Receipt-only sessionStorage expires after 24 hours. The success route also accepts the confirmed router receipt when session storage is unavailable. These browser guards are usability controls, not authorization.

## API and persistence

- `/api/health`: liveness without database access; HTTP 200 while the process is responsive.
- `/api/health/ready`: Npgsql connectivity through EF; HTTP 200 or 503. No exception or connection details appear in its JSON response. Connectivity is not proof that future schema migrations are current; migration checks are separate.
- Errors use ASP.NET Core Problem Details. Production responses do not expose stack traces; logs remain server-side. Unknown routes return Problem Details too.
- Development CORS permits configured loopback origins only. Production does not register this policy; a future production origin must be approved and explicitly configured when deployment is implemented.
- The API adds `nosniff`, `DENY` frame protection, and a no-referrer policy. Production uses HTTPS redirection and HSTS. A future reverse proxy must configure trusted forwarded headers and HTTPS correctly.

`TravelAgencyDbContext` maps travel enquiries, hashed idempotency records and notification outbox entries. The original baseline remains intact; the additive `TravelRequestSubmission` migration adds three tables and a reference sequence. Migrations run explicitly through the pinned EF tool; API startup never applies migrations automatically. Design-time tooling reads an environment connection string without inventing fallback credentials.

PostgreSQL Compose configuration is local-only: loopback port binding, persistent named volume, generated local password, and container health check. It is not production infrastructure.

## Verification boundaries

Vitest/Testing Library verify routes, pending/success/failure health states, runtime response validation, and error-boundary recovery. xUnit/WebApplicationFactory verify startup, DI, liveness, readiness success/failure, CORS, safe responses, and headers. The real database test applies the baseline twice against a dedicated test database and checks readiness.

CI uses pinned action commits, dependency lockfiles, isolated PostgreSQL, formatting/linting/type checks, builds, tests, migration checks, and package audits. No production secrets are required.

## Phase 4 persistence and notification architecture

HTTP mapping lives in Api; DTO normalization/validation and orchestration live in Application; the enquiry, contact and travel-plan records live in Domain; EF and transports live in Infrastructure. No additional runtime package was needed.

The accepted schema is version 1 of the existing TypeScript `TravelRequest`: language/service, destination, fixed or flexible dates, travellers, budget, conditional preferences, activity assistance, optional special requests and contact. An optional hidden `website` field must be empty. Unknown JSON members, invalid types and irrelevant service fields are rejected. Dates use the Swedish business calendar; audit timestamps are UTC, rounded to PostgreSQL microseconds. Contact and plan are owned values flattened into the enquiry table; child ages and stops use native arrays, while conditional preferences use JSONB. Status starts as `Submitted`. No customer account is created.

`POST /api/travel-requests` requires a UUID `Idempotency-Key`. Success and identical replays return HTTP 201 with only `referenceNumber`, `status: submitted` and `submittedAt`. The endpoint exposes no request lookup or private database ID. Invalid input returns coded Problem Details (400); a reused key with different normalized content returns 409; oversized input 413; non-JSON 415; throttling 429 with Retry-After; database failure a safe 503. The reader bounds even chunked input at 32 KiB and depth 16. No response includes submitted personal data.

A SHA-256 key hash and canonical typed-payload hash provide replay detection. A PostgreSQL transaction-scoped advisory lock serializes a key, then rechecks it inside the transaction. One commit creates the enquiry, key record and two uniquely constrained notification jobs. An injected failure test proves rollback. References use a configurable uppercase prefix plus UTC year and a global PostgreSQL sequence (`TRV-2026-000001`); gaps are expected and the sequence does not reset annually. A unique index remains authoritative. A reference is not an access credential.

The worker uses `FOR UPDATE SKIP LOCKED` to claim due jobs across workers, holds the row lock through a bounded transport call, then records its outcome. Failures retry exponentially from 30 seconds up to one hour; eight attempts by default, with permanent errors or exhaustion moved to dead-letter state. Saving an enquiry never depends on email availability. HTML output escapes user content and has a plain-text alternative. Customer templates follow the requested Swedish/English language; the agent receives an English operational summary. Notification logs contain IDs and outcomes, not message bodies or contact details.

Development Capture writes one owner-readable JSON file per notification under the API content root `.local-mail`; duplicate IDs reuse the same file. Production rejects Capture. The Resend adapter uses a ten-second HTTP timeout and the stable outbox ID as the provider idempotency key. Delivery is at least once: provider deduplication lasts 24 hours, so a crash after provider acceptance before database acknowledgement can still cause later duplicates. `SentAt` means transport acceptance/capture, not inbox delivery. No external delivery or verified sending domain has been demonstrated locally.

Idempotency records expire after 30 days by default (minimum seven); replay after expiry can become a new request. Hourly worker maintenance removes expired records. Request retention defaults to 365 days, but request deletion is disabled until `Requests__PurgeExpiredRequests=true` is explicitly approved/configured. Enabled deletion cascades notification and key rows. Capture files, backups and provider retention require separate policies. Disabling the worker also disables this maintenance. Production requires explicit retention configuration; legal basis, notices and the approved retention period remain owner decisions.

The fixed-window limiter defaults to ten submissions per IP per minute per API instance. CORS and the honeypot do not replace abuse controls. Deployment must configure trusted proxies, shared edge limits if needed, TLS, private database access and least-privilege credentials. The API never trusts arbitrary forwarded headers. Default logging excludes payloads, email addresses, connection strings and provider response bodies; sensitive-data logging must remain disabled.

Tests cover normalized validation, domain invariants, templates, real PostgreSQL concurrency/replay/rollback, worker retries/dead letters, API failures/rate limiting, frontend submission states and browser-to-database flows. Phase 5 administration and authentication are described below. Quotations, invoices, payments, bookings and deployment remain outside the current phase.

### Personal data and purpose

Enquiries collect name, email, optional phone, preferred contact/language and the requested destinations, travel dates, traveller counts/child ages, budget, preferences and optional free text. These support responding to and preparing the requested travel assistance; they do not create an account or authorize bookings. The UI discourages passport, card and sensitive information. Access is restricted to the backend/database operator and configured email recipients; no public request retrieval exists. Before launch the owner must approve a privacy notice covering controller/contact identity, lawful basis, purposes, processors/transfers, retention, deletion and data-subject rights. Database deletion must be coordinated with backups, capture files and provider copies; the technical defaults alone do not establish GDPR compliance.

## Phase 5 administrator architecture

The existing modular monolith remains in place. Domain centralizes request transitions and holds note/activity records without Identity or EF dependencies. Application defines query/mutation interfaces and DTOs. Infrastructure implements Identity persistence/provisioning, filtered queries and transactional mutations. Api owns cookie/antiforgery composition, centralized role authorization and thin endpoints. The only added package is Identity.EntityFrameworkCore 10.0.14, matching the established framework patch; no frontend dependency was added.

### Authentication and authorization

ASP.NET Core Identity stores salted framework password hashes, role membership, lockout state and security stamps in the existing PostgreSQL database. Only the `Administrator` role satisfies the shared policy. Login is generic for unknown users, wrong passwords, lockouts and unsupported second-factor accounts. Passwords require 12 characters with uppercase/lowercase/digit/symbol. Five failures lock an account for 15 minutes; the endpoint additionally permits ten attempts per IP per minute per process. No registration or password-reset endpoint is exposed.

The application cookie is HttpOnly, SameSite Strict, path `/`, without a broad domain. Production uses a `__Host-` name and Secure Always; local HTTP development uses a separate cookie name and SameAsRequest. Sessions expire absolutely after 30 minutes by default (configurable 1–120), do not slide and are browser-session cookies rather than remember-me cookies. Identity validates security stamps on every authenticated request. Logout rotates the stamp and signs out, invalidating every session for that administrator on its next request. No cookie value, password or bearer token is stored through frontend JavaScript.

`GET /api/admin/auth/csrf` issues the framework antiforgery cookie and request token. Every admin POST/PATCH, including login/logout, validates the request token from `X-CSRF-TOKEN`. The frontend fetches a fresh token before each mutation, including after an authentication identity change. Tokens live only in memory. Anonymous data access returns 401; an authenticated principal without the role returns 403. Both are distinct from 400 antiforgery/input errors. All admin responses carry no-store, and API CSP denies rendering/scripts/framing. Public submission stays anonymous and does not use administrator cookies for authorization.

Development CORS allows credentials only for explicit configured loopback origins. Vite 5173 and API 5080 are different origins but the same localhost site, so Strict cookies work with Axios `withCredentials`. A different hostname (including 127.0.0.1) is not interchangeable. Production currently has no cross-origin CORS policy; deploy behind an approved same-origin HTTPS reverse proxy or separately review an exact credentialed origin policy. Do not loosen to wildcard origins or SameSite None as a shortcut.

### Administrative API

| Endpoint | Behavior |
| --- | --- |
| GET `/api/admin/auth/csrf` | Anonymous-safe antiforgery bootstrap |
| POST `/api/admin/auth/login` | Email/password; antiforgery + IP limit; safe administrator DTO |
| POST `/api/admin/auth/logout` | Authorized, antiforgery-protected, revoke sessions; 204 |
| GET `/api/admin/auth/me` | Authorized ID, display name, email and Administrator role only |
| GET `/api/admin/dashboard/summary` | Database status counts and five latest updated requests |
| GET `/api/admin/travel-requests` | Validated server-side search, filters, deterministic sorting/pagination |
| GET `/api/admin/travel-requests/{uuid}` | Complete enquiry DTO, concurrency version and allowed next statuses |
| PATCH `/api/admin/travel-requests/{uuid}/status` | `{status, version}`; transition + optimistic concurrency; 204 or 409 |
| GET/POST `/api/admin/travel-requests/{uuid}/notes` | Paged chronological notes / `{content}`; authenticated author; 204 on creation |
| GET `/api/admin/travel-requests/{uuid}/activity` | Paged chronological immutable history |

All data/mutation endpoints require the administrator policy independently of frontend routing. Missing UUID records return 404, invalid inputs/filters 400, stale versions/concurrent writes 409. Framework Problem Details handles unexpected errors without exposing entities, hashes or exception details.

List parameters are `page` (1–100000), `pageSize` (1–100, default 20), `status` (case-sensitive enum name), `serviceType` (existing four service strings), `search` (up to 150 characters), `sort` (`createdAt_desc`, `createdAt_asc`, `updatedAt_desc`), `from`, and `to` (ISO dates, inclusive UTC submission dates). Search uses escaped, parameterized PostgreSQL ILIKE over reference/name/email. Literal percent/underscore characters are escaped. Pagination uses Count plus ordered Skip/Take in SQL, with UUID tie-breaking. Responses include items, page/pageSize, totalCount/totalPages and exclude notes/contact email from list cards. Small-scale substring search is acceptable initially; production volume may justify reviewed trigram indexes and keyset pagination.

### Workflow and persistence

`Submitted → UnderReview` or `Closed`; `UnderReview → AwaitingCustomer` or `Closed`; `AwaitingCustomer → UnderReview` or `Closed`. Closed is terminal in Phase 5 and does not mean booked. Quotation/payment/booking state remains separate and unimplemented. The domain supplies permitted transitions; APIs validate them again and the frontend displays only allowed values.

The additive `AdminDashboard` migration adds standard Identity tables, a GUID concurrency token and status constraint update, request service/updated indexes, notes and activity tables. Existing Phase 4 enquiries receive generated versions and a submitted event at their original creation timestamp. No enquiry data is deleted. Notes reference the enquiry and authenticated author, limit plain text to 4000 characters and use UTC timestamps. Activity records retain only event type, actor, previous/new status and UTC time, never the note body. Request deletion cascades notes/history; author deletion is restricted to preserve provenance. Existing retention maintenance therefore covers request-linked admin data too. Administrator-account retention requires separate operational review.

A status change updates the domain version/timestamp and adds activity in one SaveChanges transaction. Notes also touch the enquiry and record their activity atomically. EF compares the original version in UPDATE; concurrent changes roll back the entire write and return 409. No status-change emails are sent. New public submissions create their submitted activity in the existing request/outbox transaction. Public receipts and customer emails do not expose notes, admin activity or internal IDs.

### Frontend and session state

`features/admin` separates authentication context, API adapter, cancellable query hook, login/layout, dashboard/list/detail pages, fields/actions/timeline, copy and SCSS. Protected routes gate rendering after `/me`; safe return paths are restricted to known admin route shapes. Expired 401 responses clear the auth context and unmount protected data. A generation guard prevents older session checks restoring authentication after logout/invalidation. Initial page load, window focus and a 60-second check refresh the session. In-flight reads are aborted on key/route changes; stale responses cannot replace a newer query. Mutations reload detail/version and timelines; list/dashboard are freshly loaded when visited. There is no persistent private-data cache and no Redux/query dependency.

Search/filter state stays in memory (not URL, localStorage or analytics). Names, contact, notes and activity are escaped React text; there is no trusted HTML or rich-text editor. Preference labels reuse the wizard localization resources, while admin strings use the existing Swedish-default language provider. Request cards reflow instead of imposing a wide table on phones; sidebar navigation wraps into a compact navigation block on smaller screens. Admin metadata stays noindex and omits canonicals and customer names.

### Production security boundary

This phase does not claim production readiness. Implement/review standard TOTP MFA enrollment, verification, recovery codes and recovery procedures before exposing customer information. Identity token-provider/storage capabilities are retained, and a two-factor-enabled account cannot bypass its second factor through this password-only endpoint. Password recovery/rotation is a controlled operator task until a reviewed workflow exists. Do not disable MFA to work around missing UI.

Provision data-protection keys on persistent restricted storage, with encryption at rest and sharing only across intended API instances; default local key handling is insufficient for a deployment guarantee. Review HTTPS/proxy trust, secure hosts/origins, frontend admin no-store/CSP headers, session policy, centralized throttling for multiple instances, least-privilege database access, backups and account recovery. No customer analytics are introduced. Operator access is limited to authenticated administrators and database/backup operators; privacy notices/retention and legal policies still require owner approval. These technical controls alone do not establish GDPR compliance.
