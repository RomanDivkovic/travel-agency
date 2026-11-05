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

## 007 — Provisional editorial identity and reusable primitives

**Decision:** A centrally configured provisional brand, ivory/navy/sand tokens, self-hosted Cormorant Garamond and DM Sans, one Lucide icon family, semantic UI primitives, and section-level homepage components.

**Reason:** Provide a warm personal travel identity and a reusable basis for later public pages without building a large design-system framework.

**Alternatives:** External font/CDN calls, multiple icon systems, utility CSS, or an all-in-one homepage component.

**Consequences:** The brand can change in one place, typography/colors remain consistent, and local licenses accompany bundled fonts/icons. SVG compass branding is explicitly provisional. The owner's portrait remains a labeled placeholder.

## 008 — Local generated imagery and responsive loading

**Decision:** Original AI-generated illustrative travel images stored locally, responsive WebP variants, AVIF hero variants with a WebP fallback, explicit dimensions, prioritized hero loading, and lazy loading below the fold.

**Reason:** Avoid random hotlinked photography and reduce transfer size while matching the generated visual direction.

**Alternatives:** Third-party hotlinks, unoptimized PNGs, stock photos without established permissions.

**Consequences:** Scenes do not establish destination/supplier availability. An inspiration caption discloses generated imagery. Asset prompts and sources are documented in ASSETS.md; visual concepts are in docs/design. No generated text is shipped as UI.

## 009 — Translation-ready copy, safe future routes, and preserved diagnostics

**Decision:** Centralize Swedish copy without an i18n package. Use real section anchors and small labeled future-page fallbacks. Retain Phase 0 health handling at a development-only route. Keep prelaunch indexing disabled.

**Reason:** Every visible interaction needs an honest outcome without implementing later phases or displaying technical diagnostics on the public homepage.

**Alternatives:** Dead links, fake forms, premature full Services/About pages, a nonfunctional language selector, or a public health widget.

**Consequences:** Full translations and workflows remain deferred. Browser focus follows navigation, mobile navigation uses a disclosure pattern without a modal focus trap, and production excludes diagnostics. Noindex prevents reaching the suggested SEO score until an actual launch is approved; this is deliberate rather than a missing metadata implementation.

## 010 — Patched minimatch override for the lint toolchain

**Decision:** Override transitive `minimatch` versions below 11.0.1 to exact version 11.0.3. Keep the existing CI audit threshold and direct tooling versions.

**Reason:** [GHSA-g3hv-75wg-m249](https://github.com/advisories/GHSA-g3hv-75wg-m249), published to the advisory database after the original dependency checks, flags the installed 10.2.6 version. Its audit findings propagate through ESLint and typescript-eslint. Current upstream releases still constrain minimatch to 10.x, so a normal in-range update cannot resolve the advisory.

**Alternatives:** Upgrading only ESLint (still depends on vulnerable minimatch), forced audit downgrades, or weakening/disabling the audit. None addresses this dependency chain appropriately.

**Consequences:** This is a deliberate transitive major-version override, supported by the project's Node 24 runtime and verified with a clean install, the full frontend quality checks, and npm audit. Remove the override once all upstream consumers require a patched version. The version selector leaves newer, already-patched dependencies alone.

## 011 — Small, typed bilingual public site

**Decision:** Use stable English route paths for the five informational pages and a React language context backed by validated localStorage. Swedish is the default. Reuse the Phase 1 design system and preserve homepage anchors, the future-request notice and development diagnostics.

**Reason:** Two static languages do not require an i18n framework. Centralized resources and a typed shared-copy shape keep components free of duplicated language branches. Language switching preserves the current page; unavailable storage must not break the UI.

**Consequences:** The same public URLs serve both languages, so language-specific search indexing and hreflang are deferred. Document language, page metadata and all public copy change together. FAQ uses native details/summary rather than custom ARIA state. Route focus remains managed by the existing shared helper. No request wizard or new backend functionality is included.

## 012 — Explicit email drafts instead of simulated contact delivery

**Decision:** An optional public `VITE_CONTACT_EMAIL` enables a validated form that prepares an encoded mailto link. Without an approved address, fields are disabled and the page explains why. No contact-delivery API or message persistence is introduced.

**Reason:** No real mailbox or delivery provider has been supplied. An email draft is useful without claiming a message was delivered. Visitors explicitly open and send the draft in their own client.

**Consequences:** Real end-to-end delivery is unverified and depends on external email infrastructure. The form never reports success as delivery. It does not store personal data, includes data-minimization guidance and clears a prepared draft on edits. Provider-backed delivery can be added in an authorized later phase.

## 013 — Prelaunch localized SEO with an explicit public origin

**Decision:** Add route-specific localized title, description, Open Graph and Twitter metadata. Emit a canonical only from a configured HTTPS `VITE_SITE_URL`. Preserve prelaunch noindex and robots restrictions.

**Reason:** Neither an approved public domain nor launch authorization is available. Invented canonical hosts, alternate language URLs, business schema or live price claims would be misleading.

**Consequences:** This is client-rendered metadata. Search engine rendering, locale-specific URLs, prerendering/SSR, sitemap and approved indexing remain launch decisions. Owner identity, portrait, contact details, fixed fees and legal pages remain explicitly provisional.

## 014 — Reducer-driven dynamic travel wizard

**Decision:** Implement the request-preparation UI at `/plan-your-trip` with typed local reducer state, a service-dependent step list, focused step components and pure shared validation. Reuse the bilingual context and Phase 1 components. Query-based preselection accepts only four known service identifiers. Local selection changes replace the query without remounting the form; search-only updates preserve focus. This prevents an old entry link overriding a changed service on refresh, and start over removes the query.

**Reason:** Conditional steps and review edits need a single consistent answer model, without introducing a form library or future backend dependencies. Input strings preserve intermediate edits; normalized numeric values belong in the final payload.

**Consequences:** Step and whole-request validation must both pass before a complete `TravelRequest` can be produced. Service changes retain shared answers but reset service-specific preferences. Review edits revalidate all applicable steps. Progress/focus/error summaries are derived from the configured flow. No future workflow is implemented.

## 015 — Opt-in drafts with an explicit persistence allowlist

**Decision:** Save a versioned, bounded, seven-day local draft only after opt-in. Store structured travel selections and the activity-help boolean; exclude every destination/origin/stops field, contact detail and free-text request. Restore at service selection, rebuild only known keys and require omitted details again. Start over removes the draft and disables saving.

**Reason:** A full form snapshot would retain unnecessary personal data and free text on shared devices. Local storage is untrusted, unavailable in some environments and not a substitute for Phase 4 persistence.

**Consequences:** Restoration is deliberately partial, not a guarantee of full-session recovery. Expiry is enforced on the next wizard visit, not by background deletion. Storage errors are surfaced and editing continues in memory; denied deletion requires browser site-data cleanup. `useSyncExternalStore` observes write availability without setting UI state directly inside the synchronization effect. Language preference stays separate.

## 016 — Validated local payload without simulated submission

**Decision:** Define a service-discriminated TypeScript `TravelRequest` with normalized dates, travellers, budget, preferences and contact fields. A pure builder returns it only after full validation. Keep the send control disabled and explicitly disclose that nothing is sent.

**Reason:** Phase 3 prepares the information model; Phase 4 owns server contracts, persistence, real acknowledgements and email. A local valid form must not imply agency receipt or a booking.

**Consequences:** No request endpoint/client, database entity, reference number, notification, authentication, quotation or payment is added. The future API integration must independently validate and adapt this payload rather than trusting client validation. Contact details remain only in the open page.

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
- [Fontsource installation](https://fontsource.org/docs/getting-started/install)
- [Lucide React](https://lucide.dev/guide/react/getting-started)
- [WAI disclosure navigation](https://www.w3.org/WAI/ARIA/apg/patterns/disclosure/examples/disclosure-navigation/)
- [LCP optimization](https://web.dev/articles/optimize-lcp) and [Lighthouse](https://developer.chrome.com/docs/lighthouse/overview)

Phase 2 references: [React context](https://react.dev/reference/react/createContext), [React Router navigation](https://reactrouter.com/start/declarative/navigating), [localStorage](https://developer.mozilla.org/en-US/docs/Web/API/Window/localStorage), [native disclosures](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/details), and [Vite public environment configuration](https://vite.dev/guide/env-and-mode).

Phase 3 references: [React reducer](https://react.dev/reference/react/useReducer), [external store subscriptions](https://react.dev/reference/react/useSyncExternalStore), [WAI multi-page forms](https://www.w3.org/WAI/tutorials/forms/multi-page/), [WAI input validation](https://www.w3.org/WAI/tutorials/forms/validation/), and [Web Storage handling](https://developer.mozilla.org/en-US/docs/Web/API/Web_Storage_API/Using_the_Web_Storage_API).

## 017 — Atomic enquiry submission and durable replay detection

**Decision:** Adapt the existing typed payload to independently validated Application DTOs. Persist the Submitted enquiry, hashed idempotency identity and two notification jobs in one PostgreSQL transaction. Serialize identical keys with a transaction advisory lock and use a unique global sequence-backed reference. Keep the baseline; add a migration.

**Reason:** Network uncertainty and concurrent clicks must not create multiple enquiries or lose notification intent. A client validation result is not a server trust boundary.

**Consequences:** Same-key/same-normalized-payload replays return the original receipt; changed content conflicts. Sequence gaps are valid. Idempotency is retained 30 days by default; a later expired identity can create another enquiry. Public references provide no lookup authorization. This supersedes decision 016's Phase 3 disabled-submission boundary without adding customer accounts.

## 018 — Durable outbox with safe local capture and explicit external transport

**Decision:** Use PostgreSQL `FOR UPDATE SKIP LOCKED`, bounded retries/dead letters and a transport abstraction. Development captures owner-readable files; production requires a configured Resend sender/key. Templates provide encoded HTML and plain text, localized customer acknowledgement and an agent summary.

**Reason:** Email outages must not undo saved enquiries, and development must not accidentally contact customers. An accepted email is not proof of inbox delivery.

**Consequences:** At-least-once delivery can duplicate after a crash outside the provider's 24-hour deduplication window. Capture and mock HTTP tests do not establish external delivery. No email-provider SDK/package or separate queue service is needed. Retention defaults are explicit, and request purging stays off pending owner approval; capture files and backups require separate policies.

## 019 — Confirmed receipts and privacy-preserving browser retry state

**Decision:** Show success only after a validated 201 receipt. Keep a UUID/timestamp attempt across uncertain retries, seven-day browser recovery, no payload persistence, and a receipt-only 24-hour session record. Lock pending edits, map server errors to steps, honor Retry-After and require explicit reset for a new attempt.

**Reason:** A timeout can follow a successful database commit. Silent key rotation or simulated success would mislead visitors and duplicate work.

**Consequences:** Blocked attempt storage prevents submission; error feedback explains recovery. Confirmed success clears matching draft/attempt data, with explicit cleanup warnings if removal fails. General contact remains the separate mailto workflow from decision 012. Administration and subsequent business workflows remain deferred.

Phase 4 references: [ASP.NET rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0), [PostgreSQL explicit locks](https://www.postgresql.org/docs/18/explicit-locking.html), [Npgsql JSON mapping](https://www.npgsql.org/efcore/mapping/json.html), [Resend send API](https://resend.com/docs/api-reference/emails/send-email), and [Resend idempotency](https://resend.com/docs/dashboard/emails/idempotency-keys).

## 020 — Standard Identity, cookie sessions and explicit provisioning

**Decision:** Add ASP.NET Core Identity EF 10.0.14 to the existing database with a centralized Administrator policy, framework password hashing, lockout, HttpOnly Strict cookies, absolute configurable sessions, security-stamp validation and framework antiforgery tokens for every admin mutation. Provision one administrator through an explicit transactional command with supplied secrets, never a public registration endpoint or startup seed.

**Reason:** Administrative access protects customer information and requires server enforcement independent of React routing. Framework primitives avoid custom cryptography; cookie sessions avoid browser-managed bearer secrets.

**Consequences:** Development uses exact credentialed loopback origins; production requires approved HTTPS hosting, persistent encrypted key management, MFA and recovery review. Logout invalidates all sessions for the admin. Standard MFA storage/providers are available but enrollment/challenge/recovery UI is deferred; password-only login cannot bypass a required second factor. Production readiness is not claimed.

## 021 — Versioned request workflow with private notes and immutable history

**Decision:** Centralize Submitted/UnderReview/AwaitingCustomer/Closed transitions in Domain. Use a GUID EF concurrency token; atomically update status/timestamp plus activity, or create note/touch request plus activity. Add notes/history with author foreign keys and backfill submitted events for existing enquiries in an additive migration.

**Reason:** Concurrent edits must not silently overwrite decisions, and operational changes need provenance without leaking private notes.

**Consequences:** Conflicts return 409 and require review/refresh. Closed is terminal and does not imply a booking. Activity stores structured status values, never note bodies. No mutation emails or later-phase workflow states are introduced. Existing request-retention deletion cascades linked notes/history; administrator deletion is restricted where provenance exists.

## 022 — Small server-driven bilingual admin frontend

**Decision:** Reuse Axios, the language provider, SCSS tokens and buttons, with focused admin pages and a cancellable in-memory resource hook. Query/filter/page in PostgreSQL, display responsive cards, use generic metadata and exclude persistent private-data caching and search URLs.

**Reason:** The one-agent administration workflow needs readable mobile/desktop views and fresh authorized data without a second design system or state framework.

**Consequences:** Search/filter state resets when the list remounts. Mutations reload affected detail/history; dashboard/list fetch on navigation. The backend remains the authorization boundary. Future scale may justify reviewed indexed substring search or a server-state library, but neither is required for Phase 5.

Phase 5 references: [Identity configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0), [ASP.NET antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0), [EF optimistic concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency), and [Identity EF package](https://www.nuget.org/packages/Microsoft.AspNetCore.Identity.EntityFrameworkCore/10.0.14).

## 023 — Immutable quotation versions and serial commercial decisions

**Decision:** One numbered series per non-Closed request, separate version/line records, a database sequence, optimistic version tokens and a shared PostgreSQL row lock for all mutations/decisions. A partial unique index reserves one Draft/Sent/Accepted slot. Revise immediately supersedes/revokes a current Sent offer, preserving its prices/terms. Accepted cannot be revised.

**Reason:** Admin revisions, expiry and customer decisions must not create conflicting contracts or overwrite an issued snapshot. Sequential MAX+1 and browser-only guards are insufficient.

**Consequences:** A new revision is a draft and the previous link stops working immediately. Opposite decisions conflict; same-decision replay is idempotent. Terms/acceptance wording and monetary values stay frozen. Enquiry status and future invoice/payment/booking states remain separate.

## 024 — Exact money with explicit tax and legal approval gates

**Decision:** Integer two-decimal minor units for SEK/EUR/USD, integer quantities and tax basis points, backend decimal calculation and per-line midpoint-away-from-zero rounding. Support explicit exclusive/inclusive/no-tax treatment, with Unconfirmed drafts. Require approved configuration and exact configured legal-text matching before issuing.

**Reason:** No confirmed VAT treatment or reviewed consumer terms were supplied. Financial integrity and truthful offers require server authority and explicit owner/adviser input.

**Consequences:** Drafts work locally while issuance defaults off. No rate, exemption or legal waiver is assumed. A positive issued total is required. Configured terms/version and acknowledgement are frozen per version, and future changes affect only new issuance. Test fixtures are labeled nonlegal and isolated from actual business configuration.

## 025 — Fragment bearer links with hashed lookup and protected delivery envelopes

**Decision:** Generate 256-bit random version-scoped credentials; store hashes with expiry/revocation. Email `/quote#credential` links and send credentials to fixed API routes through a header. Encrypt the pending outbox delivery envelope with an isolated ASP.NET Data Protection purpose; never store a plaintext credential in the database/browser storage or expose it in admin DTOs.

**Reason:** A fragment keeps bearer secrets out of HTTP/proxy URLs while permitting account-free review. Durable delivery needs a recoverable envelope, separate from the hashed authentication lookup.

**Consequences:** Email/browser history still hold the private link and must be treated accordingly. Key persistence/encryption and capture retention are production prerequisites. Link access continues for a bounded read window after validity, while decisions independently enforce the exact deadline. Cancel/revise revoke access. Public API no-store/no-referrer, generic errors, explicit confirmation and per-IP limiting remain mandatory; no customer account or qualified-signature claim is introduced.

## 026 — Reuse outbox/activity and protect commercial retention

**Decision:** Extend existing activity/outbox records with optional quotation associations, filtered uniqueness and quotation notifications. Reuse the worker for bounded expiry batches. Exclude quotation-linked requests from the older automatic request purger until commercial retention is approved.

**Reason:** A second email/job stack is unnecessary; issued/accepted records need reliable notification intent and careful retention. An email provider outage must not change business state.

**Consequences:** Quotation Sent means issued/queued, never inbox proof. Stale issued messages can be suppressed; external delivery remains at least once. History keeps structured events/actors/versions without private notes or token values. Phase 7 receives an immutable accepted-version boundary, with no invoice/payment/activation code in Phase 6.

Phase 6 references: [ASP.NET Data Protection purpose isolation](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/purpose-strings?view=aspnetcore-10.0), [PostgreSQL row/advisory locks](https://www.postgresql.org/docs/18/explicit-locking.html), [partial unique indexes](https://www.postgresql.org/docs/18/indexes-partial.html), and [URL fragment behavior](https://developer.mozilla.org/en-US/docs/Web/URI/Reference/Fragment).

## 027 — Immutable invoices and transactional numbering

**Decision:** Create one invoice from one accepted quotation version, copy exact calculated line/tax/amount snapshots, permit billing-details editing only while Draft, and freeze approved seller/terms data at explicit issuance. Use a transactionally locked database counter for final numbering and unique constraints on number/quotation. Cancel only Draft; do not offer paid overrides, issued corrections or fabricated refunds.

**Reason:** Financial records must preserve the accepted agreement, avoid duplicate collection and not silently change issued amounts. An accountant must approve numbering, advance service billing, mandatory fields, VAT and correction/retention policy before commercial issuance.

**Consequences:** Issuance defaults disabled. Missing seller/tax/terms/accounting/provider approval returns 412. Draft cancellation does not enable another primary invoice. Authorized escaped HTML snapshots are downloadable; PDF generation is browser print/save until reliable server PDF tooling is justified.

## 028 — Disabled live payments and isolated development simulation

**Decision:** Define a focused provider interface, but implement only Disabled and a Development-only Sandbox adapter because no provider was approved. Sandbox evidence is persisted separately; every test invoice, checkout, result, email and metric is labelled simulated. Production rejects Sandbox at startup. No payment SDK/dependency or merchant credentials are invented.

**Reason:** A complete, testable financial architecture is possible without pretending that local simulation is a live/official provider sandbox or a real charge.

**Consequences:** Live provider acceptance criteria remain blocked. Future approval must establish supported currencies/methods, merchant-of-record arrangement, official hosted checkout/signatures, refund/correction obligations and real reconciliation. Simulated eligibility is separate from real travel-research eligibility and cannot activate actual service work.

## 029 — One verified, idempotent settlement path

**Decision:** Lock invoice and provider event processing, durably deduplicate event IDs, compare amount/currency/session/provider context and settle invoice/payment/eligibility/activity/outbox atomically. Reconciliation queries provider evidence and uses the same path with bounded persisted claims/retries. Success is monotonic and duplicate successful attempts become review discrepancies.

**Reason:** Redirect parameters, unsigned requests, duplicate/concurrent/out-of-order events and provider outages cannot be trusted to authorize work or revenue.

**Consequences:** Customer results fetch backend state and poll finitely. No administrative mark-paid action, raw webhook retention, late fees, debt demands or automatic reminders are introduced. Existing outbox delivers localized invoice/confirmation/failure/agent messages independently of the financial transaction.

## 030 — Invoice-scoped fragment credentials and reusable UI

**Decision:** Reuse secure-link primitives with a new invoice scope and Data Protection purpose; tokens remain in fragments and headers. Add explicit admin revocation without pretending to cancel existing payments. Reuse the admin client, CSRF/auth, language provider, SCSS tokens, in-memory resource hook and existing background worker. Lazy-load payment pages.

**Reason:** Invoice access must not be inherited from a quotation or leaked through HTTP URLs, analytics, caches or persistent browser storage. A second auth, email or state framework is unnecessary.

**Consequences:** Hosting must enforce private-page no-store/no-referrer/CSP/HTTPS and protected key persistence. Customer emails/browser history still carry private links. No new frontend/NuGet dependencies were added.

Phase 7 references: [Skatteverket invoice rules](https://www.skatteverket.se/foretag/moms/saljavarorochtjanster/fakturering.4.58d555751259e4d66168000403.html), [Verksamt invoice content](https://verksamt.se/en/agreement-invoicing/invoice/invoice-content), [PostgreSQL locking](https://www.postgresql.org/docs/18/explicit-locking.html), [.NET HMAC-SHA256](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.hmacsha256.hashdata?view=net-10.0), [fixed-time comparison](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals?view=net-10.0), [Data Protection purpose isolation](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/purpose-strings?view=aspnetcore-10.0), and [EF transactional update considerations](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete). These references inform implementation, not a declaration of legal compliance.

## 031 — Real payment evidence gates manual research

**Decision:** Reuse Phase 7 eligibility, but verify the joined real Paid invoice, accepted quotation, matched Succeeded payment and non-Closed request at every research mutation. Reject Sandbox/Disabled and simulated eligibility; provide no admin bypass. Keep research metadata/notes separate from administrative request and payment statuses.

**Reason:** Button visibility and manually edited status are not proof that paid research is authorized. Phase 7 still lacks an approved live adapter.

**Consequences:** Production research cannot be activated through the development simulator. Guarded, isolated test fixtures seed synthetic evidence without adding a production exception. The workspace reuses existing customer requirements and persists only its own metadata/options.

## 032 — Typed manual supplier options and explicit price units

**Decision:** Use one discriminated option model with typed flight/stay/extra detail snapshots, integer minor units, explicit quantity/basis and original SEK/EUR/USD currency. Keep private notes separate. Require source name, safe HTTPS link, price timestamp and conditions; availability timestamp is optional. Group flight legs by journey with explicit zone/offset consistency.

**Reason:** Manual research needs useful comparison without a supplier API, scraping, invented live inventory, accidental traveler/night multiplication, cross-currency totals or misleading return-flight duration.

**Consequences:** No supplier URL is fetched/verified by the backend. Comparison is side-by-side and responsive. New currencies need reviewed minor-unit support; no conversion source is assumed. Source credentials and automated purchases remain out of scope.

## 033 — Immutable versioned alternatives and scoped customer decisions

**Decision:** One proposal series per request, separate versions/alternatives/item associations, saved customer-safe snapshots and SQL-pagination. Require save/review after source changes before send. Freeze sent terms and prices; revise into new IDs while immediately revoking old access. Approved is terminal. Use the request row NO KEY UPDATE lock plus optimistic tokens for shared state transitions.

**Reason:** Research edits and approval/revision races cannot overwrite a customer-approved itinerary or permit approval of a replaced version. Supplier estimates and already-paid service fees must stay distinct.

**Consequences:** Explicit primary selection, unchecked acknowledgement and second confirmation precede approval. Decisions are idempotent only for the same choice. No booking/refund follows automatically; revised supplier costs require future explicit authorization. Historical source associations block destructive removal.

## 034 — Reuse secure links, outbox, preview and the existing worker

**Decision:** Proposal-only fragment/header credentials with hash lookup, isolated Data Protection envelopes, expiry/revocation, no persistent browser token storage and bounded public rate limiting. Reuse admin authentication/CSRF, activity, localized outbox, resource hooks and background expiry. Render the same saved proposal components in preview and customer views.

**Reason:** Separate scopes avoid treating invoice/quotation links as blanket customer access; reuse avoids new auth/email/job/state systems. Manual supplier links must not introduce SSRF or private-page analytics leaks.

**Consequences:** Reviewed bilingual terms and acknowledgement are required before enabling sending. Production private SPA headers/key management/privacy prerequisites still apply. Delivery/capture is not inbox proof. No Phase 9 supplier execution or portal is introduced.

Phase 8 references: [PostgreSQL lock compatibility](https://www.postgresql.org/docs/18/explicit-locking.html), [TimeZoneInfo offset validation](https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo.getutcoffset?view=net-10.0), [EF transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions), [Data Protection purpose isolation](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/purpose-strings?view=aspnetcore-10.0), and [noopener](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/rel/noopener). No supplier site was scraped or represented as automatically verified.

## 035 — Manual supplier facts, separate from customer approval

**Decision:** Generate one required supplier reservation for each selected approved proposal item and derive overall booking status. Require initial explicit external booking authority and genuine supplier confirmation/cancellation evidence. Keep internal sequence references separate from supplier references, record payment responsibility explicitly, and never execute supplier operations or alter service-fee revenue.

**Reason:** Proposal acceptance is not a supplier reservation, a successful payment attempt is not supplier confirmation, and a partially booked trip must remain visibly partial.

**Consequences:** One booking workflow per request; cancellations remain historical and terminal for that reservation. Same-category replacements/amendments use versioned recorded arrangements and renewed approval where needed. No unapproved add-on endpoint or automatic cancellation/refund exists. Flight ticketing is tracked separately from reservation confirmation.

## 036 — Conservative exact-arrangement reapproval and audit history

**Decision:** Preserve original proposal estimates and typed snapshots. Require fresh checks before start/confirm and external customer reapproval for higher price, currency/material changes or changed payment authority. Once review is triggered it cannot be cleared by unrelated edits. Reapproval records are bound to the current arrangement version, with evidence/channel/customer timestamp and administrator actor; future changes invalidate it.

**Reason:** A lower price can accompany worse conditions, and prior approval must not authorize an unrelated replacement or unlimited costs. This phase does not add a public portal or pretend that an administrator's statement is an electronic customer signature.

**Consequences:** The agent must collect genuine external approval and keep its reference. UTC approval timestamps support subsecond precision. Parent-request/booking locks, version tokens, restricted FKs and transactional before/after events protect races and preserve historical facts. [EF concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency), [EF transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions) and [PostgreSQL row locks](https://www.postgresql.org/docs/current/explicit-locking.html) informed the implementation.

## 037 — Snapshot-based booking confirmation through the existing outbox

**Decision:** Only a fully confirmed booking may queue a full-trip customer confirmation. Snapshot confirmed reservation facts and costs per booking revision. The worker locks that aggregate through delivery and discards stale confirmation jobs. Failure notifications use separate agent wording. Existing sender/backoff/capture and activity infrastructure are reused.

**Reason:** Partial trips and later amendments must not produce misleading complete-trip confirmation. Queued is not delivered; supplier details must come from recorded evidence.

**Consequences:** Local capture verifies application delivery, not an external inbox. At-least-once delivery remains possible on transport/commit uncertainty. No new job platform, upload system, payment integration or supplier fetcher is introduced.

## Phase 10 — verified customer Identity accounts and explicit ownership

- The specific Phase 10 implementation brief takes precedence over the master plan's older passwordless direction: use Identity password hashing, verified email, lockout and secure recovery. No custom credential/token cryptography or new auth provider.
- Reuse the Identity database with explicit customer markers/roles and a separate customer cookie/policy. Administrator provisioning may proceed when only customer accounts exist, but cannot promote an existing customer or overwrite an administrator. The historical `Administrator` entity name is retained to avoid unrelated Identity migration churn.
- Require an explicit request-bound email claim after verified login. Matching an email never automatically imports guest requests. Keep narrow guest tokens working independently; customer ownership is not a broadened quotation/proposal token.
- Use absolute, non-sliding sessions and security-stamp global revocation instead of a new device-session store. No pretend per-device history. Email is read-only in the profile until a separate verified-change process is designed.
- Reuse existing acceptance/payment/proposal engines through owner-checked entry points. Preserve current-version, expiry, explicit-consent, payment eligibility and concurrency rules. Never infer paid status from editable request state or simulated provider transactions.
- Project an allowlisted customer timeline in SQL and persist a safe booking-status field. Raw internal audit records and unconfirmed replacement arrangements are not customer DTOs.
- Reuse the transactional notification outbox for account messages. Action hash/purpose/expiry/consumption/revocation plus a stamp snapshot narrow access; protected delivery envelopes are cleared after processing. Local capture confirms application delivery only, not external inbox delivery.
- Keep a persistent Playwright suite in the repository. An opt-in backend fixture orchestrates browser execution against a separate `*_e2e_tests` database, cleans synthetic business/account records, and introduces no production seed endpoint or eligibility bypass. Browser traces are disabled and private email/screenshot/report artifacts stay outside Git.

Customer first/last names each retain their independent 100-character limit. Their combined value may exceed the historical 100-character administrator display-name column, so the legacy display value falls back to the first name when necessary; neither customer profile field is truncated. Registration and profile boundary tests cover this behavior.
