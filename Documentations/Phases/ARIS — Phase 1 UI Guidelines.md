# ARIS — Phase 1 UI Guidelines

**Document type:** UI Guidelines (visual design system + component specification for what's actually built in Phase 1)
**Companion documents:**
- `ARIS — Phase 1 Functional Requirements.md` — FR-x.x IDs referenced throughout this document
- `ARIS — Phase 1 Technical Documentation.md` — Angular module/routing structure these components live in
- **Mockup:** a clickable prototype of every screen described below is published as a design canvas artifact (link shared alongside this document) — treat it as the visual reference; this document is the reasoning and the reusable spec behind it.
**Status:** Draft — Phase 1 UI baseline. No brand or existing design system governed this — see §1 for the assumption this makes and how to revisit it.

---

## 1. Design Principles & Assumption

ARIS Phase 1 has no pre-existing brand or design system (greenfield project), so this document commits to an original, restrained visual direction rather than inventing a "generic app" look. The direction:

- **Calm, not clinical-sterile.** Internal healthcare tools tend toward either sterile/cold or falsely playful. ARIS aims for neither — a cool, low-saturation palette that reads as trustworthy and unhurried, appropriate for something a clinician or coder will have open for hours.
- **Dense but not cluttered.** Clinical/administrative users scan a lot of structured data (MRNs, dates, statuses). Type sizes and spacing favor information density over generous marketing-style whitespace, without ever crowding.
- **Evidence over decoration.** No illustrations, no stock-photo-style imagery, no gradient backgrounds. Every visual element (a banner, an icon, a badge) carries real meaning — nothing is decorative filler.
- **Honest about what isn't built yet.** Phase 1 screens that reference future capability (the Dashboard's placeholder, Patient Detail's "not available yet" banner) say so plainly instead of faking content — this keeps the UI trustworthy as the product grows across phases.

**If this project later adopts a corporate brand or an existing design system, this document — and the mockup — should be revisited and reconciled with it rather than run in parallel.** Everything below is this project's own system, not a derivative of an existing one.

---

## 2. Color

No existing palette to match, so this is an original set, chosen for accessibility and a calm/trustworthy feel (cool blue-teal accent, neutral slate grays, restrained warm/error accents used only where they carry real meaning).

| Token | Hex | Usage |
|---|---|---|
| `--bg` | `#F3F5F7` | Page background |
| `--surface` | `#FFFFFF` | Cards, header, sidebar, dropdowns |
| `--border` | `#DFE4EA` | Default dividers/borders |
| `--border-strong` | `#C7CFD8` | Input borders, secondary button borders |
| `--text` | `#16202B` | Primary text |
| `--text-2` | `#56677A` | Secondary text (labels, meta, subtitles) |
| `--text-3` | `#8A97A6` | Tertiary text (placeholders, disabled, icons) |
| `--accent` | `#0B6E8C` | Primary actions, links, active nav, focus ring |
| `--accent-hover` | `#075169` | Hover/pressed state of accent elements |
| `--accent-tint` | `#E5F1F5` | Active nav background, hover background, info banners |
| `--accent-tint-strong` | `#CFE7EE` | Selected-row background |
| `--error` | `#B3261E` | Error text/icons |
| `--error-tint` | `#FBEAEA` | Error banner background |
| `--error-border` | `#F1B6B0` | Error banner/input border |
| `--warn` | `#9A6B12` | Restricted-access (403) icon — deliberately amber, not red |
| `--warn-tint` | `#FBF2E2` | Restricted-access icon background |
| `--warn-border` | `#EAD3A0` | Restricted-access icon border |

**Semantic distinction that matters:** red (`--error`) is reserved for genuine failures (a search that couldn't complete, a rejected form). Amber (`--warn`) is used for access-restriction (403) — being denied access isn't a system failure, and coding it the same red as a broken search would be misleading. Don't blur this distinction as more statuses are added in later phases (e.g., gap statuses in Phase 3).

**Contrast:** `--text` on `--bg`/`--surface` exceeds 12:1. `--text-2` on `--surface` exceeds 6:1. `--accent` on white exceeds 4.5:1 for text use. All meet WCAG AA; verify again if any token value changes.

No dark theme in Phase 1 — this is a single-theme (light) system. If dark mode is wanted later, tokens are already centralized (see §8), which makes that an additive change, not a rewrite.

---

## 3. Typography

**Typeface:** IBM Plex Sans (Google Fonts) for all UI text, IBM Plex Mono for identifiers (MRN, IDs). Chosen for high legibility at small sizes, a professional/technical character without being cold, and because it's designed for dense information UI (used in IBM's own enterprise/clinical tooling). Fallback stack: `'IBM Plex Sans', system-ui, -apple-system, sans-serif` / `'IBM Plex Mono', ui-monospace, monospace`.

| Style | Size / Line-height | Weight | Usage |
|---|---|---|---|
| Page title | 22px / 28px | 600 | "Dashboard", "Patient Search" page headers |
| Section title | 18–20px / 26px | 600 | Card headings, patient name on detail view |
| Body | 14px / 20px | 400 | Default UI text |
| Body strong | 14px / 20px | 600 | Table primary column (e.g. patient name), buttons |
| Small | 12–13px / 16–19px | 400 | Meta text, helper text, secondary labels |
| Eyebrow / table header | 10–11px / 16px | 600, uppercase, +0.04–0.06em tracking | Table column headers, section labels like "MRN" |
| Mono | 13–14px | 400–500 | MRN, IDs — always monospace so digits align when scanning a list |

Never drop below 11px for any real content (table headers are the floor). Don't introduce a second UI typeface — IBM Plex Mono is the only exception, and only for identifiers/codes.

---

## 4. Spacing, Radius, Elevation

- **Spacing scale:** 4, 8, 12, 16, 20, 24, 32, 40, 48px. Pick from this scale; don't invent arbitrary values.
- **Radius:** 6px for buttons, inputs, and pills-that-aren't-fully-round; 8px for cards and panels; 999px (full) for avatar circles and status pills.
- **Borders over shadows.** The system leans on 1px borders (`--border`) for separation, not drop shadows — flatter, calmer, and avoids the "floating card" look that reads as more decorative than a dense data tool should. The one exception is transient overlays (dropdown menus, tooltips), which use a subtle shadow (`0 8px 20px rgba(16,24,32,0.14)`) since they need to visually separate from page content behind them.
- **No left-border-accent cards.** Explicitly avoid the generic "card with a colored left border" pattern for content containers — it reads as a template default, not a considered design. The one legitimate use of a left accent bar in this system is the *active navigation item* in the sidebar, which is a functional wayfinding signal, not decoration.

---

## 5. Iconography

All icons are hand-drawn inline SVG, stroke-based, 1.6px stroke weight, on a 24×24 viewBox, `stroke="currentColor"` so they inherit their container's text color. **No emoji, no icon-font glyphs** — ensures crisp rendering and consistent visual weight at every size.

Icon set used in Phase 1: grid (dashboard), people (patients), magnifier (search), chevron-down (dropdown), chevron-right (row action / breadcrumb), eye / eye-off (password visibility), lock (unauthorized), alert-triangle (error), checkmark (success / selected), clipboard (copy MRN). Extend this set with the same stroke weight and grid as new screens are added in later phases — don't mix in a different icon style.

**Product mark:** a rounded-square (accent-colored) containing a simple abstracted pulse/EKG line in white — used at 36px in the login screen and 28px in the app header. Keep this exact mark; don't introduce a second variant.

---

## 6. Core Components

### 6.1 Buttons

| Variant | Style | Usage |
|---|---|---|
| Primary | `--accent` background, white text, 40px height (38px in denser toolbars), 6px radius, 600 weight 13–14px | Main call to action (Sign in, Search, Retry) |
| Secondary | `--surface` background, `--border-strong` border, `--text` label | Secondary action (Sign in again, Clear search) |
| Icon button | No border/background by default, `--text-3` icon color, subtle `--accent-tint` hover background | Password toggle, copy button, account menu trigger |
| Pill / segmented | 999px radius, 12px 600 weight; active = filled accent, inactive = outlined | Only used for the Patient Search "preview state" demo control (see §7) — not a general-purpose pattern yet |

Minimum interactive target: buttons and nav items are sized so their effective click/tap area is at least 44px, even where the visual box is smaller (achieved via padding).

### 6.2 Inputs

Height 38px, 6px radius, 1px `--border-strong` border (switches to `--error-border` when the field is implicated in a validation error), 14px text, `--text-3` placeholder. Focus state: 2px `--accent` outline with 2px offset — this is the same focus treatment on every interactive element (buttons, links, inputs) for consistency and WCAG 2.4.7 compliance.

### 6.3 Navigation shell

Fixed 60px header (product mark + wordmark left, account menu right) and 232px sidebar (nav items), present on every authenticated screen. See §7 for the exact behavior — this isn't just a visual spec, the shell has functional rules (role-based item visibility) that must be preserved by whoever implements it in Angular.

Active nav item: `--accent-tint` background, `--accent` text, 600 weight, 3px `--accent` left border. Inactive item: `--text-2`, no background, hover → `--accent-tint`.

### 6.4 Data table (Patient Search results)

Implemented as a CSS grid, not an HTML `<table>` — five columns (`2.2fr 1fr 1fr 0.7fr 28px`: Name, MRN, DOB, Sex, row-action chevron). Header row: eyebrow style (§3), `--text-3`, bottom border. Data rows: 13px, bottom border, hover → `--accent-tint`, selected → `--accent-tint-strong` (selected state persists through hover — see the mockup's implementation for the exact CSS specificity reasoning). Name column is the only bold/primary column; MRN is monospace.

### 6.5 Pagination

"Showing X–Y of N patients" label (or "No results") + Prev/Next buttons + "Page X of Y". Prev/Next disable (visually dimmed, `--text-3`) rather than disappear at the first/last page, so the control's position never shifts.

### 6.6 Banners

Two variants used in Phase 1:
- **Info** (`--accent-tint` background, `--accent-tint-strong` border, info-circle icon) — used for "this isn't available yet" messaging (Patient Detail).
- **Error** (`--error-tint` background, `--error-border` border, alert-triangle icon) — used for form validation and search failure.

Both share the same anatomy: icon + text, 6–8px border radius, left-aligned icon at the text's cap-height.

### 6.7 Account / role menu

A single dropdown (avatar + name + role, opened from the header) rather than separate controls for "switch role" and "log out" — this keeps header chrome minimal. In Phase 1 this menu also carries the role-preview affordance described in §7; that section explains why it's labeled the way it is and why it won't ship exactly like this in the real product.

---

## 7. State & Interaction Patterns

These are the patterns Angular implementation should follow — validated against the FR-x.x IDs they satisfy.

### 7.1 Loading / Results / Empty / Error (search)

Every list-fetching view must render exactly one of four distinct states — never a blank screen standing in for "loading," never an empty table standing in for "no results" (FR-4.3, FR-4.4, FR-4.5 in the Functional Requirements document; UT-NG-06 / IT-PT-04 / E2E-06/07 in the Test Documentation):

- **Loading** — skeleton rows (animated placeholder bars matching the real row shape), not a spinner-only screen. Skeletons communicate *what's coming* better than a generic spinner for a data table.
- **Results** — normal table + pagination.
- **Empty** — centered icon + "No patients found" + a message that changes depending on whether the user had typed a search term (a blank search returning nothing reads differently than a specific search with no matches) + a "Clear search" recovery action.
- **Error** — centered warning icon (not the muted search icon used for empty) + apology-free, non-blaming copy + a "Retry" action.

### 7.2 Generic authentication errors (FR-1.2)

The login error banner must never distinguish "wrong username" from "wrong password" — always the same generic message. This is a content rule as much as a visual one: whoever writes the actual backend error copy in Phase 1 implementation must preserve this, not just the UI shell.

### 7.3 Role-based navigation (FR-2.2, FR-3.2)

The sidebar's "Patients" item is present only for Administrator, Clinician, Coder, and RiskAnalyst — Auditor and Researcher see Dashboard only, matching the RBAC matrix in the Technical Documentation. This must be enforced by the real route guard/backend (per FR-2.4), not only hidden in the nav — the mockup's role switcher is a **design/QA convenience**, not a spec for a real end-user feature; the shipped product has exactly one role per authenticated session, determined by the backend, not chosen by the user in a menu. Label any equivalent affordance in future design tools clearly as a preview tool, the way the mockup does ("Preview as — prototype only"), so it's never mistaken for real product behavior.

### 7.4 Unauthorized (FR-2.3)

The 403 page keeps the full app shell (the user *is* authenticated, just lacks permission for this one page) and states the specific missing capability in plain language, tied to their actual role — not a generic "Access Denied." Icon and color are amber/neutral (§2), not red — being denied access is an expected, calm outcome, not a system error.

### 7.5 Explicit "not built yet" messaging (FR-5.4)

Where a screen would otherwise look incomplete because a later-phase capability is missing (Patient Detail's clinical history, Dashboard's real metrics), say so directly in the UI rather than leaving a suspicious gap or, worse, faking placeholder content. This is a content pattern to reuse every time a future phase's capability is referenced before it exists.

---

## 8. Implementation Notes for Angular

- Centralize the tokens in §2–§4 as SCSS/CSS custom properties (or an Angular Material/CDK theme, if one is adopted) at the very start of Phase 1 implementation — every component below assumes shared tokens, not per-component hardcoded values.
- The shell (header + sidebar) should be one Angular component (`ShellComponent`, per the Technical Documentation's module structure) wrapping every authenticated route via a layout route, not duplicated per page — the mockup duplicates shell markup across artboards only because the design-canvas format used for prototyping has no cross-artboard component sharing; that constraint does not apply to Angular.
- The four search states (§7.1) should be an explicit state enum in the component (`'loading' | 'results' | 'empty' | 'error'`), matching the UI state model already specified in the Technical Documentation (§6.4 of that document) — the mockup's state machine is a direct reference implementation of that same model.
- Build the icon set (§5) as a shared Angular icon component/sprite once, not copy-pasted inline SVG per usage site — the mockup inlines SVG per-screen only because of the prototyping format's file-isolation constraint.

---

## 9. What's Deliberately Not Covered

- **Responsive/mobile layout.** Phase 1 targets a desktop clinical/administrative workstation (assume ≥1280px viewport); no mobile or tablet layout is specified. Revisit if a real usage need for narrower viewports emerges.
- **Dark theme.** Not built in Phase 1; tokens are centralized so it's addable later without a redesign.
- **Any screen or state beyond what Phase 1 Functional Requirements defines** — no gap statuses, no evidence display, no AI/agent UI patterns. Those get their own UI guidelines when their phase begins, following the same method this document used (design system first, then component + state specs, then explicit interaction rules tied to that phase's FR-x.x IDs).
