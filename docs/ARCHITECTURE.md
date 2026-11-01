# Architecture

## Scope

Phase 0 creates a modular monolith with a separate React frontend and a single ASP.NET Core API backed by PostgreSQL. Phase 1 builds the public homepage; Phase 2 adds bilingual public information pages without changing the backend. There are no microservices, background queues, customer models, authentication, or payment integrations. The Domain project intentionally contains no speculative entities.

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

`main.tsx` initializes React, BrowserRouter, self-hosted fonts and global styles. `app/App.tsx` places the localized error boundary inside LanguageProvider, and declares the homepage, five public pages, the future-request notice, a legacy about redirect and the not-found route within the public layout. The Phase 0 `FoundationPage` remains on the development-only `/_dev/health` route with its original health hook and success/failure/pending tests.

`app/config` owns branding and destinations; `content/sv.ts` and `content/en.ts` own typed homepage/shared copy; `content/public.ts` owns bilingual page resources; `content/images.ts` owns responsive media references. `components/ui` provides Button/ButtonLink, Container, Section, Card, and Icon; `components/layout` owns header, brand, mobile disclosure navigation, and footer. `features/home` composes separate section components, keeping the router and page composition small.

Global SCSS is limited to tokens, typography, resets, focus, and reduced-motion rules. SCSS Modules own component layouts and states. Shared SCSS breakpoints provide consistent service grids and navigation. Stateful behavior stays local: header disclosure state and route/anchor focus management. The small language context is the only shared UI state; there is no state-management or animation framework.

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
| `/plan-your-trip` | Informational notice; no wizard or submission |
| `/about-preview` | Replace redirect to `/about` for existing links |
| other paths | Localized not-found content |
| `/_dev/health` | Existing diagnostics in development only |

`features/public` separates each page from shared PageIntro, styles, copy selection and contact draft construction. Existing Section, Container, Icon and Button components remain in use. Header uses NavLink for the current page; disclosure navigation switches below 1280px to leave room for both languages. RouteFocus preserves main/anchor focus and scroll behavior, including legacy homepage anchors.

The contact form accepts a public mailbox from build-time `VITE_CONTACT_EMAIL`, validates it, and disables all fields without one. Configured mode uses native constraints plus trimmed-value validation, then exposes an encoded mailto link. It does not automatically open an email client on form submission, send HTTP requests, save personal data or assert delivery. Editing clears the prepared draft. Actual delivery requires the visitor's configured email client and mailbox infrastructure, outside this phase.

Localized page titles, descriptions, Open Graph locale/content and Twitter summary metadata update on navigation and language changes. Canonical links require a configured HTTPS `VITE_SITE_URL` and use its origin plus the current path. The provisional site retains noindex/nofollow and disallowing robots.txt. The same URLs serve both languages, so no misleading hreflang alternate URLs are emitted. Approved domain, locale-specific indexing, prerendering/SSR, sitemap, company schema and legal launch content remain deferred. No backend models, request endpoints, authentication, payments or bookings are introduced.

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
