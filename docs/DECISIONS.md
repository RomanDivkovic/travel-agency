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
