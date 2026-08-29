# ARIS Claude Code skills

Eight skills live in `.claude/skills/`. Six are ARIS-specific, built to keep a recurring project-specific workflow consistent across services/phases; two (`monday-quick-item`, `monday-ticket-creation`) are general Monday.com board-management skills that happen to be used against this project's own task board.

| Skill | Automates | Doc |
|---|---|---|
| `aris-phase-documentation` | Generating/updating a phase's full documentation set (FR, Tech Doc, Test Doc, UI Guidelines, Detailed Plan, mockups) | [aris-phase-documentation.md](aris-phase-documentation.md) |
| `aris-new-service-scaffold` | Scaffolding a new backend service to the fixed Clean Architecture + security/observability/deployment checklist | [aris-new-service-scaffold.md](aris-new-service-scaffold.md) |
| `aris-phi-safe-log-audit` | Auditing logging/exceptions for PHI-shaped fields leaking out of application code | [aris-phi-safe-log-audit.md](aris-phi-safe-log-audit.md) |
| `aris-rbac-matrix-sync` | Keeping the endpoint × role RBAC matrix in sync with actual code | [aris-rbac-matrix-sync.md](aris-rbac-matrix-sync.md) |
| `aris-branch-ticket-plan` | Reconciling the current branch's Monday ticket + the docs into a gap-analysis plan, posted to Slack | [aris-branch-ticket-plan.md](aris-branch-ticket-plan.md) |
| `aris-implementation-log` | Writing/updating living per-service `Documentations/Service Docs/` write-ups of how the code works and connects (with a Mermaid mind map), plus a combined test-description doc | [aris-implementation-log.md](aris-implementation-log.md) |
| `monday-quick-item` | Creating one Monday.com board item from a title + description | [monday-quick-item.md](monday-quick-item.md) |
| `monday-ticket-creation` | Bulk-creating Monday.com board items from a requirement document | [monday-ticket-creation.md](monday-ticket-creation.md) |

## Skills vs. agents vs. hooks

A skill is instructions the *main conversation* follows while doing the work itself — it doesn't spawn a separate reviewing persona (that's what `.claude/agents/` is for) and it doesn't run as an automated gate on a tool call (that's `.claude/hooks/`). The six ARIS skills exist because each covers a workflow specific enough, and repeated often enough across this project, that re-deriving it from first principles each time would be wasteful or drift-prone:

- `aris-phase-documentation` and `aris-new-service-scaffold` are **generative** — they produce new documents/code following an established pattern.
- `aris-phi-safe-log-audit` and `aris-rbac-matrix-sync` are **auditing** — they check existing work against a rule, but (unlike an agent) they're meant to be followed by whoever's already in the conversation doing the work, not delegated out.
- `aris-branch-ticket-plan` is **reconciling** — it doesn't generate a new artifact or audit code against a fixed rule; it cross-checks three moving sources (a Monday ticket, the doc set, the branch's own diff) against each other and reports the gap.
- `aris-implementation-log` is **explanatory** — it doesn't check code against a rule or generate a new artifact from a spec; it turns code that already exists into a living, connected explanation of how it works, for `Documentations/Service Docs/`.

## Why these six, and not more

These were selected from a broader candidate list surveyed against the project's documentation set, deliberately kept to patterns the docs state will recur across *every* phase or *every* service — not one-off conveniences. Three more candidates were identified but explicitly deferred until their owning phase actually starts, per the project's own "don't build ahead of the current phase" rule: `aris-audit-event-scaffold` (Phase 2+, once services beyond IdentityService have their own auditable actions), `aris-raf-model-version-scaffold` (Phase 3, once RAF calculation exists), and `aris-agent-tool-registration` (Phase 4, once agent tools exist to register).

## How the six ARIS skills cross-reference each other

- `aris-new-service-scaffold` is the base — its §1 (BuildingBlocks) is what `aris-phi-safe-log-audit` checks is actually *used*, and its §2 security checklist is what `aris-rbac-matrix-sync` checks is actually *wired in* for a newly scaffolded service.
- `aris-rbac-matrix-sync` and `aris-phi-safe-log-audit` both eventually get invoked against the same services, but check disjoint concerns (authorization correctness vs. PHI-safe logging) — using one doesn't substitute for the other.
- `aris-phase-documentation` is upstream of two others in one sense: it's what produces (and cascades changes to) the very documents (Technical Documentation §1.3, §5.3, FR doc) `aris-phi-safe-log-audit`, `aris-rbac-matrix-sync`, and `aris-branch-ticket-plan` all treat as source of truth.
- `aris-branch-ticket-plan` sits a level above the auditing/RBAC pair in scope — its own gap analysis names `aris-rbac-matrix-sync`, `aris-phi-safe-log-audit`, or the relevant agent as the recommended next step rather than re-deriving their checks inline, so it composes with them instead of duplicating them.
- `aris-implementation-log` shares its ticket-from-branch extraction convention with `aris-branch-ticket-plan` (rather than re-deriving it), but runs the opposite direction — `aris-branch-ticket-plan` is forward-looking (ticket + docs → what's still missing), while `aris-implementation-log` is backward-looking (code that already exists → how it works and connects). It also names `aris-rbac-matrix-sync`, `aris-phi-safe-log-audit`, and the `auth-session-security-reviewer` agent as follow-ups rather than re-deriving their checks when a ticket's diff touches their territory. Its `<Service>-Tests.md` output is complementary to, not a substitute for, the `fr-techdoc-testdoc-traceability-auditor` agent — this doc describes what each existing test does; that agent checks whether every FR *has* a test in the first place.

## Invocation model

Every skill can be reached two ways:

- **Explicit** — the user types `/<skill-name>` (e.g. `/aris-new-service-scaffold`), optionally with arguments after it, or asks in plain language ("use the aris-rbac-matrix-sync skill on this endpoint"). Either form loads the skill's instructions into the current turn.
- **Implicit** — before every turn, Claude Code hands the assistant the name + one-line description of every skill in `.claude/skills/`. The assistant is expected to call a skill on its own, without being asked, whenever the task at hand matches what that skill's description says it's for — no user action triggers this, it's a judgment the assistant makes each turn. This is why each skill's description is written as a trigger condition ("use whenever...", "use before finishing any change that...") rather than just a summary: that phrasing *is* the implicit-invocation rule. See each skill's own doc for its specific trigger wording.

Nothing about a skill's own file marks it as "auto-invoke only" or "explicit only" — every skill in `.claude/skills/` is eligible for both paths simultaneously; which one actually happens on a given turn depends on whether the user asked by name or the assistant recognized the match itself.

## Why the two Monday skills are split the way they are

`monday-quick-item` and `monday-ticket-creation` aren't ARIS-specific in the same sense — they're general Monday.com board-management skills, split by *scale of intent* rather than by domain: one item from a title+description vs. bulk-importing many items from a requirement document. Both default to this project's Monday "Tasks" board, which is the only ARIS-specific detail either one carries.
