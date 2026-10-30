# Phase 1 assets

## Provenance

Travel scenes and design concepts were generated specifically for this project using the built-in Image Gen tool. They are illustrative images, not licensed photographs of a named destination, proof of supplier availability, or photographs of the owner. No random search-result images are hotlinked. An on-page inspiration note discloses generated imagery. The owner's portrait is intentionally a code-native, labeled placeholder.

Production media lives in `apps/web/src/assets/images`. Original PNGs remain in the local Codex generated-images directory; the repository contains optimized AVIF/WebP derivatives. Images use explicit dimensions and responsive source sets. The coast's native maximum width is 1122px; its source-set descriptor uses that actual width. The hero is preloaded/prioritized, while inspiration images are lazy-loaded. The small decorative hero inset has empty alternative text.

| Scene | Original generated filename | Production variants |
| --- | --- | --- |
| Mediterranean coast | `exec-b6e5f36e-5a63-44e7-b29d-9d9a5ce4e239.png` | AVIF 480/960/1122; WebP 480/960/native |
| European city | `exec-c74cc608-8fdd-43b1-bb93-f9aa068d0acf.png` | WebP 480/960/1440 |
| Quiet beach | `exec-8cff8f9f-b333-4646-ad2c-e88c68e8fea9.png` | WebP 480/960/1440 |
| Family promenade | `exec-730d987a-a8b5-4a82-bc14-adbb0816d2d3.png` | WebP 480/960/1440 |

The AVIF hero variants are about 45/145/181 KiB; the WebP fallback is retained for compatibility. Conversion/resizing used the bundled Sharp runtime, without adding a runtime image-processing dependency to the application.

## Final asset prompts

### Coast

```text
Use case photorealistic-natural. Original editorial travel photograph for a warm approachable Swedish travel planner website. Vertical 4:5 composition, a beautiful generic Mediterranean harbor village perched along a turquoise cove, sun-washed peach and cream buildings with terracotta roofs, green hills, stone quay, one small wooden boat. View from elevated winding coastal path, lush foreground vegetation, natural late afternoon Mediterranean light, authentic attainable holiday atmosphere, subtly analog film-like colors, crystal-clear teal sea, no tourists in foreground. Upper half village and soft blue sky, lower half cove and boat. No luxury yachts, no resort, no text, no lettering, no graphic elements or borders. Sharp magazine-quality realistic photographic detail. Generic illustrative location, not specific named destination.
```

### City

```text
Use case photorealistic-natural. Original editorial travel photograph for Swedish travel inspiration card. Landscape3:2, inviting European canal city at golden morning light with traditional pastel facades and bicycles, small stone bridge over still canal, relaxed attainable city-break atmosphere, natural subtle film texture, warm amber and muted blue tones coherent with Mediterranean magazine travel imagery. No recognizable branded signage, no lettering, no overlay, no border, no watermarks. Not an advertisement for specific real destination. Realistic high detail professional photography.
```

### Beach

```text
Use case photorealistic-natural. Original editorial travel photograph, landscape3:2, quiet Mediterranean sandy cove turquoise clear sea, weathered limestone cliffs and olive trees in distance, one simple striped parasol beside two empty beach chairs far away for scale, relaxed attainable holiday not luxury resort, sunny natural light, warm sand and teal water. Natural authentic colors magazine-quality photorealism. No people, no text, no logos, no borders, no watermarks.
```

### Family

```text
Use case photorealistic-natural. Original editorial travel photograph landscape3:2. Peaceful family-friendly European seaside promenade with a small warm-colored coastal town, tall pine trees providing dappled shade, calm blue water and a small pebble beach, two distant adults and a child seen only from behind strolling together, faces not visible, leisurely authentic attainable family holiday. Early evening warm sun, warm neutral colors and ocean blues, natural magazine photography, beautiful lived-in place rather than luxury resort. No text logos watermarks borders or graphic elements.
```

## Design references

The complete visual direction is preserved as four readable references in `docs/design`:

- [Opening and hero](design/phase-1-opening.webp), native 1505×1045.
- [Services and process](design/phase-1-services-process.webp), native 1487×1058.
- [Benefits and personal introduction](design/phase-1-personal.webp), native 1320×1192.
- [Inspiration, closing, and footer](design/phase-1-closing.webp), native 1036×1518.

These references use ivory/navy/sand, serif headings, clean body typography, large travel imagery, restrained cards, open process steps, and a clearly labeled owner portrait placeholder. The written Phase 1 specification overrides generated copy errors, invented proof, and incidental graphic details; deliberate corrections are recorded in [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md).

## Fonts and icons

DM Sans and Cormorant Garamond are self-hosted from Fontsource with SIL Open Font License notices included under `apps/web/public/licenses`. Lucide supplies the single outline icon family; its ISC/MIT notice is included alongside the fonts. The provisional compass favicon is a small local SVG. The browser makes no external font, icon, or image CDN requests.
