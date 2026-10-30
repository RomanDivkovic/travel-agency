# Phase 1 design system

Working brand: **Your Travel Planner**, configured in `apps/web/src/app/config/branding.ts`. This is an editable placeholder, not an approved legal business identity.

## Visual specification

The implementation follows four generated visual references: opening/header/hero; services/process; benefits/personal introduction; inspiration/closing/footer. The references are visual direction, not substitutes for the approved requirements or code-native UI. Generated text that differs from the Phase 1 prompt is replaced by the supplied Swedish copy. No invented claims, prices, testimonials, biography, or contact information are permitted.

The selected reference images are stored in [docs/design](design), with provenance and prompts in [ASSETS.md](ASSETS.md).

- Background is warm ivory `#FAF8F5`, not white. Surfaces are white; the process and agent sections use sand `#EDE5D8`; the closing band uses navy `#173B57`.
- Navy headings and controls; accessible muted body text; teal links and small terracotta accents. All repeated values belong to tokens.
- Cormorant Garamond 500 for display/headings; DM Sans 400/500/600 for body/controls, self-hosted Latin fonts including Swedish characters. Avoid external font requests.
- Desktop container approximately 1280px, fluid side gutters, generous section spacing, serif headline spanning two lines beside arched coastal imagery.
- Hero image has **no overlay or tint**. A smaller landscape image overlaps its lower-left edge at larger widths. Inspiration images also have no overlay.
- Four white service cards use subtle borders, restrained corners, and one consistent outline icon family. The four process steps are open columns, not boxed cards.
- Benefits use a heading beside an open 2×2 list. The personal section uses an explicitly labeled portrait placeholder rather than a fictional person.
- Inspiration uses three image-led themes with descriptive copy and no prices or availability. A navy CTA band precedes a quiet four-column footer.

## Components and ownership

Shared primitives: Button/ButtonLink (primary, secondary, outline, text; small/medium/large), Container, Section, Card, Icon, BrandLogo. Typography is expressed through centralized CSS tokens and semantic elements rather than a wrapper around every text node.

Shell: Header with disclosure-style mobile navigation, Footer, skip link, and route/anchor focus management. Major homepage sections are separate components. `App` stays routing/composition glue. All customer-facing Swedish copy belongs to `content/sv.ts`; routes and branding have dedicated configuration modules.

Icons use Lucide only: compass for the provisional brand, plane/bed/luggage/compass for services, heart/clock/user/wallet for benefits, arrows for actions, and menu/close for mobile navigation. Decorative SVGs are hidden from assistive technology. The provisional logo deliberately uses a simple scalable compass, not the intricate raster mark in the concept.

## Copy and interaction inventory

Allowed above-the-fold copy: the configured brand; Hem, Tjänster, Så fungerar det, Om mig, Kontakt; “Din nästa resa börjar här.”; the supplied hero paragraph; “Planera min resa”; “Se våra tjänster”. No hero eyebrow, badge, statistic, booking widget, or language selector is added.

Navigation goes to real homepage anchors. The shared `/plan-your-trip` destination clearly says the request form is coming in a later phase and accepts no submissions. The about CTA uses a small informational fallback, not a Phase 2 page. Contact anchors reveal an honest launch placeholder. Legal placeholders are plain text rather than broken links.

Keep service fees distinct from travel costs, explain payment before paid research and customer approval before booking, and make no claim of instantly confirmed travel.

## Responsive and accessible behavior

Mobile-first layout: one service column below 768px, two on tablets, four from 1024px; process steps stack on mobile; inspiration stacks on small screens. Header navigation becomes a disclosure below 1024px. Menu state is announced, Escape returns focus to its trigger, selection closes it, and resize/outside interaction clears stale open state. It does not create a modal or trap focus.

Use visible focus rings, native controls, a skip link, semantic headings/landmarks, descriptive image alternatives, explicit image dimensions, responsive WebP assets, lazy loading below the fold, and reduced-motion support. Sticky-header anchors have scroll margins. Verify keyboard/focus behavior and contrast in addition to automated axe checks; do not claim screen-reader/device certification from automated checks.

## Intentional corrections to generated references

1. Use the user's exact copy, including the paid-service workflow; ignore generated copy errors and added section eyebrows.
2. Use one restrained button radius across hero and closing CTA instead of mixing rectangular and pill-shaped controls.
3. Use a simple provisional compass mark and the single configured brand name; omit invented taglines.
4. Assets are original AI-generated illustrative travel scenes, not evidence of specific destinations or supplier availability. Keep the agent portrait a labeled placeholder.
5. Keep prelaunch noindex behavior until publication is explicitly approved. Homepage metadata is present; production indexing and full public-page SEO remain later work.
6. Give service CTAs descriptive visible labels rather than repeating “Läs mer”, and use 16px descriptive body copy for readability. The supplied hero copy is unchanged.
