---
name: aris-phase-documentation
description: Generate or update the full documentation set and UI mockups for an ARIS phase (Phase 2 onward), following the exact pattern established for Phase 1 — Functional Requirements, Technical Documentation, Test Documentation, UI Guidelines, Detailed Plan, a multi-artboard mockup, and an interactive walkthrough prototype. Use whenever starting a new phase's documentation from scratch, or when a scope/requirement change needs to be cascaded consistently across an existing phase's entire doc set and both mockups (not just one file).
---

# ARIS phase documentation set

Phase 1's five documents and two mockups, all in `Documentations/Phases/` and `Layouts/Phase 1/`, are the canonical template. Don't re-derive structure from first principles — open the Phase 1 equivalent of whatever you're writing and mirror its section skeleton, table columns, and level of rigor. This skill exists to keep that pattern from drifting phase to phase, and to make sure a change touches every artifact it needs to, not just the one the request mentioned.

## The deliverable set (one phase = all of this)

In `Documentations/Phases/`, named `ARIS — Phase <N> <Doc Type>.md`:

1. **Functional Requirements** — what the system must do, zero implementation detail. Skeleton: Purpose/Scope → Actors in Scope → Functional Requirements by area (`FR-<section>.<n>` table: ID/Requirement/Actor/Acceptance Criteria/Priority) → User Stories → Business Rules → Non-Functional (user-facing) Requirements → Out of Scope → Functional Acceptance/DoD → Traceability.
2. **Technical Documentation** — architecture, data, API, security, deployment. Skeleton: Architecture Overview → Technology Stack → Data Design (full table DDL, indexes) → API Design (concrete request/response JSON per endpoint, RBAC matrix) → Security Design → Angular Technical Design (module structure, routing table, state model) → Deployment Design (Compose topology, env vars) → Sequence Flows → Observability → Traceability (FR → design element) → Explicit Non-Goals.
3. **Test Documentation** — verification plan. ID schemes: `UT-<area>-##` (unit), `IT-<area>-##` (integration), `E2E-##`, `SEC-##`, `PERF-##`. Skeleton: Strategy Overview (pyramid) → Environments/Tooling → Unit/Integration/E2E test tables → Security-Relevant Test Cases → Performance Checks → Test Data Strategy → CI Integration → Defect Tracking → Traceability (FR → test IDs, every FR must appear) → Exit Criteria.
4. **UI Guidelines** — visual system + component/interaction specs. Reuse the existing design tokens (color, type, spacing) from the prior phase's UI Guidelines rather than re-deriving them — only add new components/patterns the new phase actually introduces. Skeleton: Design Principles → Color/Typography/Spacing/Iconography (carry forward, don't restate unless changed) → Core Components → State & Interaction Patterns (tied to FR IDs) → Implementation Notes for Angular → What's Deliberately Not Covered.
5. **Detailed Plan** — execution plan, not design. Skeleton: Objective → Architecture (phase-scoped diagram) → per-service/data model breakdown → Angular app structure/screens → Docker Compose → Testing Strategy (summary only, points to Test Documentation) → Work Breakdown table with dependencies → Exit Criteria → Risks → Deliverables Checklist (subset of the master spec's §117-style checklist).

Every doc's header carries: **Document type**, **Companion documents** (the other 4 + the mockup, by exact filename), **Source** (master-spec `§` references), **Status**. Keep these lists in sync — adding a doc means adding it to every other doc's companion list.

Also touch, when the phase's scope is new or changes materially: the whole-project `Documentations/Holy Grail/ARIS — Technical Documentation.md` (cross-phase traceability table, service catalog) and `Documentations/Holy Grail/ARIS — Project Plan.md` (phase row, milestone table, effort estimate — **re-baseline and say so explicitly** when scope grows; never silently absorb added scope into an old estimate).

## Mockups

Two artifacts per phase, both published and both copied into `Layouts/Phase <N>/`:

- **Multi-artboard canvas mockup** — one artboard per screen in the FR doc's screen inventory, built with the `design` skill. Each screen's own state machine (loading/results/empty/error where relevant), demo affordances clearly labeled as prototype-only, real client-side validation matching the FR's acceptance criteria (e.g., generic error messages, anti-enumeration behavior, session-revocation-on-sensitive-action). Reuse the exact color/type tokens from the UI Guidelines doc.
- **Interactive walkthrough** — a single self-contained HTML/vanilla-JS file (hash router, no framework, no design-canvas tooling) covering the same screens but with *real* cross-screen navigation and auth/role guards, since the canvas format can't navigate between artboards. This is what actually proves a multi-step flow (e.g., admin action → forced re-login → gated screen) end to end.

Ask before building if it's ambiguous whether static mockups or a clickable prototype are wanted — don't assume. Before publishing either: run `node --check` on extracted `<script>` content and a plain-text tag-balance check; after any non-trivial logic (a new modal, a new guarded route, a new form), spawn a background review agent to trace the actual code against the intended behavior rather than eyeballing it — this has caught real bugs every time it's been used on this project. When asked to confirm an artifact reflects a change, `action: "read"` the *live* published URL and grep the saved local copy for concrete markers — don't answer from memory.

After publishing, copy both HTML outputs into `Layouts/Phase <N>/`, plus a `canvas-source/` subfolder with the canvas mockup's individual `.dc.html` artboards and `canvas.json` (needed to re-seed it later without starting over).

## Cascading a change

The hardest part isn't writing one document — it's a single new/changed requirement touching all seven artifacts consistently. When scope changes (new requirement, removed requirement, changed mechanism):

1. Update the Functional Requirements doc first — this is the source of truth for *what* changed.
2. Technical Documentation: data model, endpoint(s), sequence flow(s), RBAC matrix, traceability row.
3. Test Documentation: new/changed test IDs, traceability row — every FR needs at least one test.
4. UI Guidelines: new/changed component or interaction pattern section.
5. Detailed Plan: work-breakdown task(s), risk(s) if applicable, deliverables checklist, re-baselined estimate if the change is non-trivial.
6. Both mockups: rebuild the affected logic in each independently (they don't share code) — grep both for the old behavior's identifiers afterward to confirm nothing was left half-migrated.
7. Sweep every doc for the specific old terminology the change made obsolete (e.g., grep the whole `Documentations/` tree) — a changed mechanism often leaves stale wording in a "business rules" or "risks" bullet that a targeted edit misses. One past pass found a risk-table row that flatly contradicted the new design because it was never revisited.

Don't skip a layer because the request only mentioned the UI or only mentioned the API — if it's a real requirement change, it belongs in the FR doc regardless of where the request entered.
