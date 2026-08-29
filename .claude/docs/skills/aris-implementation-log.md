# aris-implementation-log

**Definition:** `.claude/skills/aris-implementation-log/SKILL.md`

## What it does

Writes or updates living, per-service docs under `Documentations/Service Docs/`: an architecture/implementation doc that explains how newly written code works, how it connects to the rest of the service, and which design patterns / architecture patterns / tech-stack mechanics it exercises — including a Mermaid diagram and a plain-text summary as a "mind map" of the flow — plus a combined test-description doc detailing what every backend and frontend test for that service actually verifies.

## How it does it

**§1 scopes the work**: identify the ticket from the branch name (reusing `aris-branch-ticket-plan`'s extraction convention), pull the branch's full diff against `main` (not just the latest commit), group changed paths by top-level unit (`src/Services/<X>/` → `<X>.md`, `src/BuildingBlocks/` → `BuildingBlocks.md`, gateway → `Gateway.md`), and read the actual changed files plus enough surrounding context (callers, DI wiring) to describe connections accurately rather than guessing. Angular changes are never dumped into one catch-all UI file — they're grouped by the backend feature area they consume and written into a paired `<Service>-UI.md`, kept as a separate doc from `<Service>.md` and cross-linked to it (and vice versa), mirroring `CLAUDE.md`'s vertical-slice rule without collapsing the API and UI explanations into one write-up.

**§2 handles the first run for a unit**: since there's no prior log entry to append onto, the doc's living sections (1–4: purpose, architecture-at-a-glance with a Mermaid layer diagram, a design-patterns/tech-stack table, and per-flow Mermaid diagrams) must cover the *entire unit as it exists today*, not just the current ticket's diff. A fixed skeleton is given so every unit's doc has the same shape.

**§2a defines the third file, `<Service>-Tests.md`**: unlike the impl/UI pair, this one is combined across backend and frontend tests rather than split per side, and it's living-only — no append-only log section, just a current, re-synced-each-run inventory of every test and what it verifies (test suites at a glance, a Mermaid coverage map linking each flow to the test IDs that cover it, and a per-test detail table citing each test's own `// UT-ID-xx`/`// FR-x.x` comment where one exists, never inventing an ID for a test that doesn't carry one).

**§3 handles every subsequent run**: living sections (1–4) are edited in place only where something actually changed — no restating an unchanged pattern or redrawing an unchanged flow. Section 5 (the log) is prepended with a new dated, ticket-attributed entry (newest first); a second run mid-ticket replaces that ticket's existing entry rather than duplicating it. Each entry documents, per meaningfully-touched file: its responsibility and Clean Architecture layer, its connections (cited `file:line`), the specific pattern/tech-stack mechanics involved (explained in terms of this codebase, not textbook definitions), and any non-obvious decisions. Step 5 folds in the Tests doc: if the ticket touched any test, reconcile `<Service>-Tests.md`'s tables/coverage map to match — no dated entry, since that doc has no history section, only a current state to keep accurate.

**§4 sets cross-unit and cross-skill boundaries**: multi-unit branches get one doc update per unit, cross-linked rather than duplicated — the API/UI pairing is this same rule applied within one vertical slice (`IdentityService.md` for the endpoint contract and server-side flow, `IdentityService-UI.md` for the Angular side, each linking to the other instead of restating it). The Tests doc is deliberately *not* split the same way — combined because test coverage is one cross-cutting concern, and splitting it would just fragment one picture across two files. This skill explicitly does not replace `aris-rbac-matrix-sync`, `aris-phi-safe-log-audit`, the `auth-session-security-reviewer` agent, or the `fr-techdoc-testdoc-traceability-auditor` agent (the Tests doc explains what each existing test does; that agent's FR-x.x ↔ test-ID matrix checks whether every requirement *has* one — complementary, not a substitute either direction) — it names them as follow-ups when relevant instead of re-deriving their checks. It also maintains a one-line-per-unit index at `Documentations/Service Docs/README.md`, listing all files (impl, UI, tests) a unit has.

**§5 sets the reporting bar**: state plainly which doc(s) were touched (impl, UI, and/or Tests), which sections/rows changed, and flag anything that couldn't be explained confidently from what was actually read.

## Why it exists

ARIS is built solo, phase-by-phase, with the actual design detail living in phase Technical Documentation — but that documentation describes *intended* design, not the as-built reality of how a given ticket's code actually turned out, nor how it connects to code from earlier tickets. Without a running, connected explanation, rebuilding that mental model after time away (or onboarding a teammate later) means re-reading diffs and cross-referencing spec documents from scratch. This skill exists to make that unnecessary: one doc per service that stays current, reads as a narrative rather than a changelog, and gives a visual (Mermaid) and textual mind map of how the pieces fit — grounded in the actual code, not restated design intent.

## When it fires

Whenever a ticket's implementation looks finished (before wrapping up, opening a PR, or moving to the next ticket), or whenever the user says **"document the ticket"** or **"document the changes"** (or asks to log/explain/mind-map what was just built, or to document the tests).

## How to invoke

- **Explicitly**: `/aris-implementation-log`, or the phrases the skill is keyed to — "document the ticket," "document the changes" — or plain-language equivalents ("explain how this connects," "give me a mind map of what we just built," "document the tests").
- **Implicitly**: the assistant should apply this skill on its own when a ticket's work looks complete (tests passing, ready to wrap up) even without those exact phrases, the same way `aris-phi-safe-log-audit` fires "before finishing any change" rather than waiting to be asked.

## Other details

- **Complements, doesn't replace, `aris-branch-ticket-plan`** — that skill reconciles a branch against its ticket/docs and produces a *forward-looking* gap plan posted to Slack; this skill produces a *backward-looking* explanation of code that already exists, kept in the repo as `Documentations/Service Docs/`. They share the same ticket-ID-from-branch convention (§1) so it isn't re-derived twice.
- **As-built, not spec** — deliberately distinct from the phase Technical Documentation, which stays the source of truth for intended design. This skill's doc can and should describe deviations from that intent when the code legitimately diverged, rather than silently matching the spec's wording.
- **Depends on reading real code, not the ticket title** — every pattern/connection claim must trace to a file:line actually read in this run; a claim that can't be grounded should be flagged in the final report, not smoothed over.
- **Living-section discipline is the main failure mode to guard against** — without the "edit in place, don't restate" rule in §3, the overview sections would either go stale (never updated after the first run) or balloon with redundant copies of the same diagram/table across many log entries. The Tests doc carries this discipline furthest — it has no log section at all, so every run must reconcile it to current reality rather than only ever appending.
- **The Tests doc's IDs are taken from the code, never invented** — a test's `// UT-ID-xx`/`// FR-x.x` comment (already a convention in this codebase's `AuthenticationServiceTests.cs`/`AuthControllerTests.cs`) is copied verbatim into the table; a test with no such comment gets a blank/"no ID" cell rather than a guessed traceability ID, keeping this doc from silently drifting into an inaccurate stand-in for the phase's real traceability matrix.
