# ARIS Claude Code skills

Six skills live in `.claude/skills/`. Four are ARIS-specific, built to keep a recurring project-specific workflow consistent across services/phases; two (`monday-quick-item`, `monday-ticket-creation`) are general Monday.com board-management skills that happen to be used against this project's own task board.

| Skill | Automates | Doc |
|---|---|---|
| `aris-phase-documentation` | Generating/updating a phase's full documentation set (FR, Tech Doc, Test Doc, UI Guidelines, Detailed Plan, mockups) | [aris-phase-documentation.md](aris-phase-documentation.md) |
| `aris-new-service-scaffold` | Scaffolding a new backend service to the fixed Clean Architecture + security/observability/deployment checklist | [aris-new-service-scaffold.md](aris-new-service-scaffold.md) |
| `aris-phi-safe-log-audit` | Auditing logging/exceptions for PHI-shaped fields leaking out of application code | [aris-phi-safe-log-audit.md](aris-phi-safe-log-audit.md) |
| `aris-rbac-matrix-sync` | Keeping the endpoint × role RBAC matrix in sync with actual code | [aris-rbac-matrix-sync.md](aris-rbac-matrix-sync.md) |
| `monday-quick-item` | Creating one Monday.com board item from a title + description | [monday-quick-item.md](monday-quick-item.md) |
| `monday-ticket-creation` | Bulk-creating Monday.com board items from a requirement document | [monday-ticket-creation.md](monday-ticket-creation.md) |

## Skills vs. agents vs. hooks

A skill is instructions the *main conversation* follows while doing the work itself — it doesn't spawn a separate reviewing persona (that's what `.claude/agents/` is for) and it doesn't run as an automated gate on a tool call (that's `.claude/hooks/`). The four ARIS skills exist because each covers a workflow specific enough, and repeated often enough across this project, that re-deriving it from first principles each time would be wasteful or drift-prone:

- `aris-phase-documentation` and `aris-new-service-scaffold` are **generative** — they produce new documents/code following an established pattern.
- `aris-phi-safe-log-audit` and `aris-rbac-matrix-sync` are **auditing** — they check existing work against a rule, but (unlike an agent) they're meant to be followed by whoever's already in the conversation doing the work, not delegated out.

## Why these four, and not more

These were selected from a broader candidate list surveyed against the project's documentation set, deliberately kept to patterns the docs state will recur across *every* phase or *every* service — not one-off conveniences. Three more candidates were identified but explicitly deferred until their owning phase actually starts, per the project's own "don't build ahead of the current phase" rule: `aris-audit-event-scaffold` (Phase 2+, once services beyond IdentityService have their own auditable actions), `aris-raf-model-version-scaffold` (Phase 3, once RAF calculation exists), and `aris-agent-tool-registration` (Phase 4, once agent tools exist to register).

## How the four ARIS skills cross-reference each other

- `aris-new-service-scaffold` is the base — its §1 (BuildingBlocks) is what `aris-phi-safe-log-audit` checks is actually *used*, and its §2 security checklist is what `aris-rbac-matrix-sync` checks is actually *wired in* for a newly scaffolded service.
- `aris-rbac-matrix-sync` and `aris-phi-safe-log-audit` both eventually get invoked against the same services, but check disjoint concerns (authorization correctness vs. PHI-safe logging) — using one doesn't substitute for the other.
- `aris-phase-documentation` is upstream of the other three in one sense: it's what produces (and cascades changes to) the very documents (Technical Documentation §1.3, §5.3, FR doc) the other three skills treat as their source of truth.

## Invocation model

Every skill can be reached two ways:

- **Explicit** — the user types `/<skill-name>` (e.g. `/aris-new-service-scaffold`), optionally with arguments after it, or asks in plain language ("use the aris-rbac-matrix-sync skill on this endpoint"). Either form loads the skill's instructions into the current turn.
- **Implicit** — before every turn, Claude Code hands the assistant the name + one-line description of every skill in `.claude/skills/`. The assistant is expected to call a skill on its own, without being asked, whenever the task at hand matches what that skill's description says it's for — no user action triggers this, it's a judgment the assistant makes each turn. This is why each skill's description is written as a trigger condition ("use whenever...", "use before finishing any change that...") rather than just a summary: that phrasing *is* the implicit-invocation rule. See each skill's own doc for its specific trigger wording.

Nothing about a skill's own file marks it as "auto-invoke only" or "explicit only" — every skill in `.claude/skills/` is eligible for both paths simultaneously; which one actually happens on a given turn depends on whether the user asked by name or the assistant recognized the match itself.

## Why the two Monday skills are split the way they are

`monday-quick-item` and `monday-ticket-creation` aren't ARIS-specific in the same sense — they're general Monday.com board-management skills, split by *scale of intent* rather than by domain: one item from a title+description vs. bulk-importing many items from a requirement document. Both default to this project's Monday "Tasks" board, which is the only ARIS-specific detail either one carries.
