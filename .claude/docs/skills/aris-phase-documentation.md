# aris-phase-documentation

**Definition:** `.claude/skills/aris-phase-documentation/SKILL.md`

## What it does

Generates, or cascades a change consistently across, a full phase's documentation set: Functional Requirements, Technical Documentation, Test Documentation, UI Guidelines, Detailed Plan, and two mockup artifacts (a multi-artboard canvas mockup and a self-contained interactive walkthrough). Phase 1's five documents and two mockups are the canonical template — every later phase is built by mirroring Phase 1's section skeleton, table columns, and level of rigor, not by re-deriving structure from first principles.

## How it does it

**For a new phase**, it produces all seven artifacts to the fixed skeleton each document type follows (e.g., Functional Requirements: Purpose/Scope → Actors → `FR-<section>.<n>` tables → User Stories → Business Rules → NFRs → Out of Scope → Traceability; Technical Documentation: Architecture → Stack → Data Design → API Design → Security → Angular Design → Deployment → Sequence Flows → Observability → Traceability → Non-Goals), and copies both mockups into `Layouts/Phase <N>/` alongside the canvas mockup's editable source (`canvas-source/` with individual `.dc.html` artboards and `canvas.json`).

**For a change to an existing phase**, it follows a fixed seven-step cascade so a single new/changed requirement doesn't land in only the document the request happened to mention:
1. Functional Requirements doc first (source of truth for *what* changed).
2. Technical Documentation (data model, endpoints, sequence flows, RBAC matrix, traceability row).
3. Test Documentation (new/changed test IDs, traceability row).
4. UI Guidelines (new/changed component or interaction pattern).
5. Detailed Plan (work-breakdown task, risk, deliverables checklist, re-baselined estimate if non-trivial).
6. Both mockups, rebuilt independently (they don't share code) — then grepped for the old behavior's identifiers to confirm nothing was left half-migrated.
7. A sweep of the whole `Documentations/` tree for the specific old terminology the change made obsolete — the skill notes a past pass found a stale risk-table row that flatly contradicted a new design because it was never revisited.

Every document's header must carry a Companion Documents list (the other four docs + the mockup, by exact filename) — adding a document means adding it to every other document's companion list too. When a phase's scope is new or changes materially, it also touches the whole-project `Technical Documentation.md` and `Project Plan.md` (cross-phase traceability, phase row, milestone table) — and explicitly re-baselines the effort estimate rather than silently absorbing added scope into an old number.

Before publishing either mockup: `node --check` on extracted `<script>` content and a tag-balance check; after any non-trivial logic change (a new modal, guarded route, form), a background review agent traces the actual code against intended behavior rather than eyeballing it — the skill notes this has caught real bugs every time it's been used on this project.

## Why it exists

The project is built "phase-by-phase, as a set of ASP.NET Core microservices... solo" (CLAUDE.md), and every phase after Phase 1 is expected to produce the identical five-document-plus-two-mockup set Phase 1 established. Without a skill enforcing this, a solo developer under time pressure will naturally let the pattern drift — skip a companion-document update, forget to re-baseline an estimate after scope grows, or let a mockup diverge from the FR doc it's supposed to represent. The skill's own framing states the core problem directly: "The hardest part isn't writing one document — it's a single new/changed requirement touching all seven artifacts consistently."

## When it fires

- Starting a new phase's documentation from scratch (Phase 2 onward).
- Any scope or requirement change that needs to cascade consistently across an existing phase's entire doc set and both mockups — not just the one file the request happened to mention.
- Explicitly *not* for a change confined to a single document with no requirement-level implication (e.g., a pure prose clarification) — though the skill is written to err toward treating an ambiguous case as requiring the full cascade rather than assuming it's contained.

## How to invoke

- **Explicitly**: `/aris-phase-documentation`, or ask directly — "write Phase 2's documentation set," "cascade this requirement change across Phase 1's docs."
- **Implicitly**: the assistant is expected to load this skill on its own whenever it recognizes either trigger condition in the skill's own description — "starting a new phase's documentation from scratch," or "a scope/requirement change needs to be cascaded consistently across an existing phase's entire doc set and both mockups (not just one file)." In practice, that second condition is the one worth watching for: a request that sounds narrow ("update the FR doc to add X") but is actually a requirement change should trigger this skill implicitly even though the user only mentioned one document — the skill's whole point is catching exactly that case.

## Other details

- **Ask before building mockups** if it's ambiguous whether static mockups or a clickable prototype are wanted — the skill explicitly says not to assume.
- **Verification discipline before trusting an artifact reflects a change**: read the *live* published URL and grep the saved local copy for concrete markers, rather than answering from memory of what was asked for.
- **This is the skill that produces the very documents the other three ARIS skills treat as ground truth** — `aris-new-service-scaffold`'s §1.3 folder convention, `aris-rbac-matrix-sync`'s per-phase RBAC matrix section, and `aris-phi-safe-log-audit`'s PHI-shaped field inventory table all live in documents this skill is responsible for keeping consistent.
- **Doesn't itself write application code** — it's purely a documentation/mockup skill; the mockups it builds are prototypes (vanilla HTML/JS or a design-canvas artifact), not the real Angular application.
