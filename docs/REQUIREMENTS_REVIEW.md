# Requirements review

Reviewed: 2026-10-29. Status: planning only; Phase 0 has not started.

## Source and scope

[MASTER_PLAN.md](MASTER_PLAN.md) preserves the supplied master specification unchanged. Implement only the phase explicitly assigned in a separate prompt. Optional future features remain outside Phases 0–15.

The workspace was an empty Git repository on an unborn `master` branch at review time. There was no application, test suite, or existing architecture to assess. Nothing has been staged or committed.

## Delivery milestones

| Milestone | Phases | Reviewable result |
| --- | --- | --- |
| MVP 1 — Online presence | 0–4 | Public information and persistent travel requests |
| MVP 2 — Business operations | 5–7 | Administration, quotations, invoicing, and service payments |
| MVP 3 — Complete booking platform | 8–15 | Proposals, booking records, portal, documents, communication, and production readiness |

Milestone completion is not permission to launch. A request-only public release needs an explicitly scoped subset of the privacy, security, and deployment work from Phases 14–15. Payments and bookings must remain disabled until their applicable launch gates are satisfied.

## Foundation direction for the Phase 0 prompt

- React with strict TypeScript, an ASP.NET Core REST API, and PostgreSQL; one modular monolith with clear frontend/backend boundaries.
- Prefer .NET 10 LTS for a new foundation. Microsoft's current support policy lists support through November 2028; verify supported patch versions when implementing.
- Evaluate a Vite frontend against public-page SEO and rendering requirements before locking the build setup. React documents Vite as an option for building from scratch, while highlighting the need to decide routing, data fetching, and rendering strategy.
- Pin selected toolchain versions, keep secrets out of Git, document local configuration, and provide reproducible development and CI commands.
- Verify frontend-to-API communication and an actual PostgreSQL connection. Include builds, linting, formatting checks, type checking, and a small meaningful test suite.
- Limit the UI to a foundation verification screen. Branding, homepage sections, customer workflows, authentication, and payments belong to later phases.

Sources checked during review:

- [Microsoft .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [React: Build a React app from scratch](https://react.dev/learn/build-a-react-app-from-scratch)

## Dependencies to resolve within the relevant phase

1. **Request access before Phase 5:** Phase 4 retrieval must not create a public listing or permit access by guessable reference alone. Test retrieval internally until authorized administration exists.
2. **Quotation access before Phase 10:** Phase 6 needs narrowly scoped, expiring customer access and an acceptance audit record. The full customer portal can still wait until Phase 10.
3. **Configuration before Phase 13:** Prices and business details need a minimal configuration source as soon as public pages use them. Owner-facing content editing remains Phase 13.
4. **Security throughout:** Server validation, authorization, personal-data minimization, safe logs, and secret handling start when their features are introduced. Phase 14 consolidates and verifies them.
5. **Payment integrity in Phase 7:** Service fees and travel costs remain separate. Verified, idempotent provider events must govern payment activation; browser redirects cannot prove payment. Research must remain blocked until the service payment is confirmed.
6. **Booking integrity in Phase 9:** Track individual supplier bookings and failures independently so a partially booked trip cannot become fully confirmed by mistake.
7. **Localization:** Swedish is the default and English the secondary language. Plan localization boundaries early; implement translated public pages in Phase 2. Display illustrative fees only as clearly qualified estimates until the owner validates pricing.
8. **Operational readiness for MVP 1:** With the dashboard deferred to Phase 5, define a secure interim way for the owner to handle requests before any public request-only release. Notification emails should avoid unnecessary personal data.
9. **Privacy in the wizard:** Define retention, collection notices, and handling of free-text sensitive information before accepting real submissions. Avoid collecting passport details or unnecessary child identifiers for initial inquiries.

## Decisions that can wait

The brand, domain, owner biography, final prices, payment provider, email provider, hosting provider, and legally approved travel-payment arrangement are not prerequisites for this requirements review. Use explicit placeholders or disabled integrations when appropriate; never invent business claims or successful transactions.

## Review outcome

The roadmap is coherent with the dependencies above made explicit. The next authorized step is a separate Phase 0 implementation prompt. No application code, infrastructure, packages, database, or CI configuration has been created during this review.
