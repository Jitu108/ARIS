---
name: aris-implementation-log
description: Write or update living, per-service docs under "Documentations/Service Docs/" — an architecture/implementation doc explaining how the code just written works, how its pieces connect, and which design patterns / architecture patterns / tech-stack mechanics it exercises (with a Mermaid + plain-text mind map of the flow), plus a combined test-description doc detailing what every backend and frontend test for that service actually verifies. Use whenever a ticket's implementation looks finished (before wrapping up, opening a PR, or moving to the next ticket), or whenever the user says "document the ticket" or "document the changes" (or asks to log/explain/mind-map what was just built, or to document the tests).
---

# ARIS implementation log

Every ticket lands code; this skill turns that code into a doc a reader (the solo dev returning in six months, or a new teammate) can use to rebuild the mental model without re-reading every file. It is **as-built and educational**, not a spec — it explains what the code *actually does* and *why it's connected the way it is*, grounded in file:line citations, not a restatement of the Functical/Technical Documentation's design intent.

One living Markdown file per touched service/unit lives under `Documentations/Service Docs/<Unit>.md` (e.g. `IdentityService.md`, `BuildingBlocks.md`, `Gateway.md` — named after the top-level folder under `src/` it documents). Each file has an always-current overview (sections 1–4) and an append-only, newest-first change log (section 5). Don't create one file per ticket — update the existing per-service file.

**API and UI are always separate docs, cross-linked, never merged.** A ticket that ships a vertical slice (backend endpoint + its Angular feature, per `CLAUDE.md`'s "vertical slices only" rule) still gets *two* doc updates, not one combined write-up: the backend doc `<Service>.md` for the API/service side, and a paired `<Service>-UI.md` for the Angular feature area that consumes it (e.g. `IdentityService.md` + `IdentityService-UI.md`). Each doc's §1 carries a one-line "API/UI counterpart" pointer to the other (`Frontend counterpart: see [IdentityService-UI.md](./IdentityService-UI.md)` / `Backend counterpart: see [IdentityService.md](./IdentityService.md)`), and any place one side would need to explain the other's internals, link instead of duplicating.

**A third file, `<Service>-Tests.md`, documents what the tests actually verify — combined across backend and frontend, not split per side.** Unlike the impl/UI docs, this one is **living-only** (no append-only log, see §2a) — it's a standing, always-current inventory of every test for the service and what scenario/assertion each one covers, kept in sync as tests are added/changed/removed. It's cross-linked from both `<Service>.md` and `<Service>-UI.md`, and they're cross-linked back from it.

## 1. Figure out what to document

1. **Identify the ticket**, reusing `aris-branch-ticket-plan` §1's convention: `git branch --show-current`, strip known prefixes, extract the `[A-Z]+-\d+` token. If asked ad hoc ("document the changes") with no ticket-shaped branch, ask what to attribute the entry to rather than inventing a ticket ID — an entry can be dated instead of ticketed if there truly isn't one.
2. **Get the branch's full implementation, not just uncommitted work**: `git log main..HEAD --oneline` and `git diff main...HEAD --stat` (same as `aris-branch-ticket-plan` §3) — a ticket's doc entry covers everything the branch did, not just the latest commit.
3. **Group changed paths by top-level unit** — everything under `src/Services/<X>/` documents into `<X>.md`; `src/BuildingBlocks/` into `BuildingBlocks.md`; the Ocelot gateway project into `Gateway.md`. Angular changes (once the workspace exists) are grouped by which backend feature area they consume — not dumped into one catch-all UI file — and documented into `<Service>-UI.md` (e.g. an Identity login/user-management component set → `IdentityService-UI.md`), paired with that service's own `<Service>.md`. `tests/` changes (backend xUnit projects) and frontend `*.spec.ts`/e2e changes both attribute to whichever service/unit they test, feeding into that unit's `<Service>-Tests.md` (§2a) rather than the impl or UI doc directly — though a log entry in the impl/UI doc can still *mention* that tests were added for a change, without duplicating each test's detail there. A branch touching multiple units, or both sides of a vertical slice, gets one updated doc per unit/side, cross-linked (§4).
4. **Read the actual changed files** (`git diff main...HEAD` for full content, not just the stat summary) plus enough of their surrounding, unchanged code (callers, DI registration, base classes) to explain connections accurately. Every claim in the doc must be traceable to something read, not inferred from the ticket title.

## 2. First time documenting a unit: write the full baseline

If `Documentations/Service Docs/<Unit>.md` doesn't exist yet, section 5 has nothing to append *onto* — so sections 1–4 must cover the **entire unit as it stands today**, not just this ticket's diff (read the whole unit's current source tree, not only the diff, for this one case). Use this skeleton:

```markdown
# <Unit> — Implementation Log & Architecture Guide

_Last updated: <date> · <TICKET-ID>_

## 1. What this is for
<1–2 paragraphs: functional purpose, owning phase tag, what database/external deps it owns>
<API/UI counterpart line: `Frontend counterpart: see [<Service>-UI.md](./<Service>-UI.md)` on an API doc,
 or `Backend counterpart: see [<Service>.md](./<Service>.md)` on a UI doc — omit only if the other side
 genuinely doesn't exist yet>

## 2. Architecture at a glance
<Clean Architecture layers present (Domain/Application/Infrastructure/Api), key folders per layer>

\`\`\`mermaid
graph TD
  Api --> Application
  Infrastructure --> Application
  Application --> Domain
\`\`\`

<plain-text summary tree of the folder/dependency structure, for a no-render fallback>

## 3. Design patterns & tech-stack building blocks in use
| Pattern / tech item | Where | Why it's used here |
|---|---|---|
| ... | `path/File.cs` | one line, specific to this codebase, not a textbook definition |

## 4. Key flows
<one Mermaid sequence/flow diagram per primary end-to-end flow the unit implements
 (e.g. login, token refresh + rotation, logout/revocation, user deactivation) —
 these are the "mind map" the user asked for: trace a request from entry point to
 persistence and back, naming the actual classes/methods involved>

## 5. Implementation log (newest first)

### <TICKET-ID> — <short title> (<date>)
...
```

Section 3's table and section 4's diagrams only ever *grow or get corrected* on later runs — don't restate an unchanged pattern/flow's row/diagram verbatim in the log entry below it; the log entry references it instead ("see §3 Rotation-on-use row", "see §4 Refresh flow diagram, updated below").

## 2a. The Tests doc: `<Service>-Tests.md`

One combined file per service, covering **both** backend xUnit suites and frontend `*.spec.ts`/e2e suites for that vertical slice — not split per side, unlike the impl/UI pair. It has no append-only log section; it's living-only, always describing the *current* complete test inventory (re-synced each run, the same way sections 1–4 of the impl doc are). Use this skeleton the first time it's written for a service:

```markdown
# <Service> — Test Documentation

_Last updated: <date> · <TICKET-ID>_

## 1. What this covers
<1–2 paragraphs: which test projects/files this documents (backend UnitTests/IntegrationTests
 project names, frontend spec files), and how it differs from the phase's own Test Documentation
 traceability matrix (that's FR-x.x ↔ test-ID coverage, owned by the
 fr-techdoc-testdoc-traceability-auditor agent; this doc is as-built — what each test actually
 does — not a traceability audit)>
Backend doc: see [<Service>.md](./<Service>.md) · Frontend doc: see [<Service>-UI.md](./<Service>-UI.md)

## 2. Test suites at a glance
| Project / file | Type | What it exercises | Key fixture/harness |
|---|---|---|---|
| ... | unit / integration / frontend unit | one line | e.g. fakes used, WebApplicationFactory, TestBed |

## 3. Coverage map
\`\`\`mermaid
graph TD
  Flow1[Login flow] --> UT1[UT-ID-01 ...]
  Flow1 --> IT1[IT-ID-01 ...]
\`\`\`
<one node per primary flow from the impl doc's §4, linked to every test ID that covers it —
 this is the "mind map" for test coverage: which flows are actually verified, and by what>

## 4. Test-by-test detail (grouped by suite/file)
### <TestClassName.cs / testFile.spec.ts>
| Test | Requirement/Test ID | Scenario | Assertion | Where |
|---|---|---|---|---|
| `MethodName_Scenario_ExpectedResult` | UT-ID-xx / FR-x.x (from the test's own comment, if present) | what's arranged/acted | what's asserted | `file:line` |
```

Keep the requirement/test IDs in the table exactly as they appear in the test's own comment (e.g. `// UT-ID-01: ...`, `// FR-1.2: ...`) — don't invent an ID for a test that doesn't carry one, just leave that column blank or note "no ID" rather than guessing at phase traceability.

## 3. Every other run: update the living sections, then append the log entry

1. **Sections 1–4 (living)**: edit in place only where this ticket actually changed something true about the unit — a new pattern introduced, a flow that changed shape, a new layer/folder. Don't rewrite a section that's still accurate. If a flow diagram changed, update that diagram directly rather than leaving a stale one plus a new one.
2. **Section 5 (log)**: prepend a new `### <TICKET-ID> — <short title> (<date>)` entry (most recent first). If an entry for this exact ticket ID is already at the top (skill re-run mid-ticket after more commits), replace that entry rather than adding a second one for the same ticket.
3. Each log entry should cover, for every file the ticket meaningfully touched:
   - **Responsibility**: what this class/module is for, and which Clean Architecture layer it lives in.
   - **Connections**: who constructs/calls it (DI registration site, calling controller/service), what it depends on, and what data flows in/out — cite `file:line`.
   - **Pattern/tech-stack nitty-gritty specific to this change**: name the actual pattern (Repository, Result/problem-details wrapper, optimistic concurrency via `RowVersion`, refresh-token rotation-with-reuse-detection, options pattern, middleware pipeline stage, EF Core fluent configuration, etc.) and explain *how this codebase's version of it works*, not the generic definition.
   - **Notable decisions or gotchas**: anything non-obvious a reader would otherwise have to reverse-engineer (e.g. why a revoke cascades the whole token chain, why a field is nullable, a deliberate deviation from the obvious approach).
4. If the ticket's flow is new or changed, add/update the relevant Mermaid diagram in §4 as part of this same entry — the log entry text should say what changed in the diagram, not just repeat the diagram.
5. **`<Service>-Tests.md` (living, no log)**: if the ticket added, changed, or removed any test (backend or frontend), reconcile the Tests doc to match the current test code — add new rows to §2/§4's tables, correct any row whose scenario/assertion no longer matches what the test does, remove rows for deleted tests, and update §3's coverage map if a flow gained or lost coverage. Don't append a dated entry for this — the doc has no history section, only a current state to keep accurate. If the ticket touched no tests at all, leave this file untouched.

## 4. Cross-unit and cross-skill boundaries

- If a branch touches multiple units, each unit's doc explains its own side; cross-link with a relative Markdown link (`See [BuildingBlocks.md](./BuildingBlocks.md) §3 for the Result pattern this controller returns`) instead of re-explaining shared building blocks in every doc that uses them.
- The API/UI pairing is the same rule applied to one vertical slice: `IdentityService.md` explains the endpoint contract and server-side flow; `IdentityService-UI.md` explains the Angular component/service/interceptor side and how it calls that endpoint — each links to the other rather than restating request/response shapes or backend logic on the UI side (or component/state details on the API side).
- `<Service>-Tests.md` is combined (backend + frontend together) precisely because it's a single cross-cutting concern — unlike the impl/UI split, splitting tests by side would just fragment one coverage picture across two files for no reader benefit.
- **`<Service>-Tests.md` is not the phase's Test Documentation traceability matrix.** That matrix (FR-x.x ↔ Test-ID, owned by the `fr-techdoc-testdoc-traceability-auditor` agent) tracks whether every requirement *has* a test; this doc explains what each test that already exists *actually does*, in as-built detail. They're complementary — this doc is a good source to consult when auditing traceability, but running this skill is never a substitute for that agent's audit, and vice versa.
- This skill documents **how the code works and connects** — it does not replace or re-derive the checks other skills/agents own. If the ticket touched a protected endpoint, changed JWT/refresh/session mechanics, or touched an entity with identifying fields, name `aris-rbac-matrix-sync`, the `auth-session-security-reviewer` agent, or `aris-phi-safe-log-audit` respectively as separate follow-ups rather than folding their checks into this log.
- Maintain `Documentations/Service Docs/README.md` as a one-line-per-unit index (unit name, one-line purpose, last-updated ticket) — create it if missing, update the relevant row after every run. List all three files for a unit that has them (impl, UI, tests).

## 5. Reporting

After writing, tell the user plainly: which doc(s) were created vs. updated (impl, UI, and/or Tests), the ticket/entry attributed, and which sections (living vs. log, or — for the Tests doc — which rows) changed. If something in the diff couldn't be explained confidently from what was read (e.g. a call site outside the diff wasn't inspected, or a test's actual assertion wasn't fully traced), say so rather than guessing at the connection.
