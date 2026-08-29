---
name: aris-branch-ticket-plan
description: Analyze the current git branch against its Monday.com ticket and the ARIS documentation set, then draft and post a change plan to Slack. Use whenever asked to "analyse/check what's needed on this branch against Monday", "check the ticket for this branch", or otherwise asked to reconcile a branch's ticket + the docs into a plan and post that plan to Slack — as opposed to `monday-quick-item`/`monday-ticket-creation` (those create Monday items, they don't read the board or post to Slack).
---

# Branch → ticket → doc reconciliation → Slack plan

Reconciles three things for the branch currently checked out: what the branch's ticket says (Monday), what the docs say is actually in scope (the doc map in `CLAUDE.md`), and what the branch's own commits/diff currently do — then turns the gaps into a plan and posts it to Slack.

## 1. Identify the ticket from the branch

- `git branch --show-current` for the branch name (e.g. `TARIS-013`, `dev/TARIS-046`).
- Strip known prefixes (`dev/`, `feature/`, `bugfix/`, …) and extract the ticket token with a `[A-Z]+-\d+` match (this repo's convention — see `TARIS-011`, `TARIS-012`, `TJKG-020` in `git log`/`git branch -a`).
- If the branch name has no ticket-shaped token, ask the user for the ticket ID rather than guessing which item it might be.

## 2. Find and read the matching Monday item

- Default board is the ARIS "Tasks" board (id `5030912763`, same default `monday-quick-item`/`monday-ticket-creation` use) unless the user names a different one.
- Search for the ticket token (`mcp__claude_ai_monday_com__search`, or `get_board_items_page` filtered on item name) — don't assume the item name is the bare ticket ID verbatim, items are often named `<TicketID>: <summary>`.
- If nothing matches, say so and ask for the item URL/ID rather than picking the closest-looking item.
- Once found, read: status, priority/type columns, the native description field, and recent updates/comments (`get_updates`) — this is "what the ticket actually asks for," which is often more current than the branch name alone.

## 3. Read what the branch actually contains

- `git log main..HEAD --oneline` and `git diff main...HEAD --stat` for committed work; `git status`/`git diff` for anything uncommitted.
- This is "what's already been done," to be diffed against "what's asked for" in steps 2 and 4 — don't re-derive it from memory of the conversation if it's been a while, re-read it fresh.

## 4. Cross-reference the documentation

Follow the doc map in `CLAUDE.md`, in order, but only as deep as the ticket's subject matter requires:

1. `Documentations/Holy Grail/ARIS — Complete Implementation and User Reference Documentation.md` (§ matching the ticket's feature area) and the Project Plan, for overall intent and sequencing.
2. `Documentations/Holy Grail/ARIS — Technical Documentation.md` for the target end-state design — check the component's **phase tag** before proposing anything; never suggest work tagged to a later phase.
3. The current phase's doc set under `Documentations/Phases/` (today, only Phase 1 exists) — FR doc (for the FR-x.x this ticket implements), Technical Documentation (API/schema/security detail), Detailed Plan (task order/exit criteria), Test Documentation (what must be tested), UI Guidelines (if the ticket touches Angular).
4. Explicitly check the relevant phase doc's **Non-Goals** section — a suggestion that belongs to a later phase is a defect in the plan, not a nice-to-have to mention.

## 5. Find the gap and draft the plan

Compare ticket intent (step 2) + doc spec (step 4) against actual branch content (step 3). Call out:
- What the ticket/docs require that the branch doesn't have yet.
- Any of this project's cross-cutting non-negotiables that look unaddressed for this change (PHI-safe logging, independent JWT/RBAC validation per service, versioning, audit events) — per `CLAUDE.md`'s "Non-negotiable principles."
- Where an existing skill/agent is the right next step rather than free-form advice — e.g. `aris-rbac-matrix-sync` if an endpoint changed, `aris-phi-safe-log-audit` if logging touches an identifying entity, the `auth-session-security-reviewer` agent if this touches JWT/refresh-token/session mechanics, the `fr-techdoc-testdoc-traceability-auditor` agent if an FR changed. Name them as recommended next steps, don't just re-derive their checks inline.

Structure the plan as: ticket summary (ID, title, status) → current branch state (commits/diff summary) → gap analysis → proposed changes (ordered, each tied to the FR-x.x or doc section it satisfies) → open questions, if any.

## 6. Post the plan to Slack

- Post via the same webhook already configured for this project (`SLACK_WEBHOOK_URL` in `.claude/settings.local.json`'s `env`): `curl -s -X POST -H 'Content-Type: application/json' -d "$(jq -n --arg t "$PLAN_TEXT" '{text:$t}')" "$SLACK_WEBHOOK_URL"`.
- Post the structured plan itself (step 5's output), not a one-line summary — that's the deliverable being asked for.
- The user's own request to "plan the changes and post to Slack" is the authorization for this specific post; no separate confirmation prompt is needed each time this skill runs this way. If the drafted plan surfaces something that changes the scope of what's being posted (e.g. it recommends action on another team's ticket), flag that before posting rather than after.
- Note for context, not action: this project's Stop hook (`.claude/hooks/slack-notify.sh`) separately mirrors every turn's final chat response to the same Slack webhook. That's independent, pre-existing infrastructure — it is not this skill's Slack post and shouldn't be relied on as a substitute for it (its content is whatever the turn's closing text happens to be, not the structured plan).

## Notes

- If the branch's ticket is already `Done`/closed on Monday, say so plainly rather than manufacturing a gap analysis for it — report that the ticket and branch appear reconciled.
- If Monday and the docs disagree (ticket asks for something the current phase's docs mark as a non-goal), surface that conflict explicitly rather than silently picking one side.
