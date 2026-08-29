# aris-branch-ticket-plan

**Definition:** `.claude/skills/aris-branch-ticket-plan/SKILL.md`

## What it does

Reconciles three sources for the branch currently checked out: what its Monday.com ticket asks for, what the ARIS documentation set (per `CLAUDE.md`'s doc map) says is actually in scope, and what the branch's own commits/diff currently contain — then turns any gap into a structured plan and posts that plan to Slack.

## How it does it

**§1–2 identify the ticket and read it from Monday**: extract the ticket token (`[A-Z]+-\d+`) from the branch name, stripping prefixes like `dev/`; search the ARIS "Tasks" board (id `5030912763`, the same default `monday-quick-item`/`monday-ticket-creation` use) rather than assuming the item name is the bare ticket ID; read its status, columns, native description, and recent updates — treating this as the current source of truth, since a ticket's description can move ahead of a branch name coined at checkout time.

**§3 reads the branch itself**: `git log`/`git diff` against `main` for committed work, plus `git status` for anything still uncommitted — re-read fresh rather than trusting anything already in conversation context.

**§4 cross-references the docs** in the order `CLAUDE.md` establishes: Holy Grail docs for intent/sequencing, the Technical Documentation for target design (checking each component's phase tag), the current phase's FR/TechDoc/DetailedPlan/TestDoc/UIGuidelines set for the concrete spec, and explicitly the phase's Non-Goals section — a plan item that belongs to a later phase is treated as a defect in the plan, not a bonus suggestion.

**§5 drafts the plan**: ticket summary → branch state → gap analysis → ordered proposed changes (each tied back to an FR-x.x or doc section) → open questions. Gaps that match an existing skill/agent's job (an endpoint changed → `aris-rbac-matrix-sync`; logging touches an identifying entity → `aris-phi-safe-log-audit`; JWT/refresh/session mechanics → the `auth-session-security-reviewer` agent; an FR changed → the `fr-techdoc-testdoc-traceability-auditor` agent) are named as the recommended next step rather than re-derived inline.

**§6 posts to Slack** via the same `SLACK_WEBHOOK_URL` already configured in `.claude/settings.local.json`, posting the full structured plan (not a one-line summary) — that's the deliverable the invocation asked for.

## Why it exists

The project runs ticket-per-branch (`TARIS-011`, `TARIS-012`, `TARIS-013`, ...), a Monday "Tasks" board, a six-document-per-phase spec, and a Slack webhook already wired for turn notifications — but nothing previously tied those four together into "is this branch actually finishing what its ticket and the docs say it should, and what's left." Re-deriving that reconciliation by hand each time a branch is picked up risks missing a doc-tagged non-goal, missing a ticket update that moved the goalposts after the branch was cut, or silently skipping the Slack visibility step the workflow depends on.

## When it fires

Whenever asked to analyze what's left on the current branch against its ticket and the docs, check a branch's ticket against the Monday board, or otherwise reconcile branch + ticket + docs into a plan that gets posted to Slack.

## How to invoke

- **Explicitly**: `/aris-branch-ticket-plan`, or ask directly — "check this branch's ticket against Monday and the docs, plan what's left, post it to Slack."
- **Implicitly**: the assistant should recognize a multi-clause request that names checking the current branch's ticket on Monday, checking documentation, and posting a plan to Slack, even when phrased informally and without the skill's name.

## Other details

- **Distinct from the two Monday skills**: `monday-quick-item`/`monday-ticket-creation` *write* to the board (create items); this skill only *reads* the board (find and inspect the one item matching the current branch) — it never creates or modifies a Monday item as part of its own flow.
- **Distinct from the Stop hook's Slack mirroring** (`.claude/hooks/slack-notify.sh`): that hook independently posts whatever text closes out *any* turn, regardless of this skill. This skill's own Slack post is the structured plan itself, sent explicitly in step 6 — not a byproduct of the hook, and not a substitute for it either.
- **Depends on the phase doc set staying current** — if `aris-phase-documentation` hasn't been run for a scope change, this skill's doc cross-reference will be checking against a stale spec; it surfaces contradictions between the ticket and the docs rather than silently trusting either.
