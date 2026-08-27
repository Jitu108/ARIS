# monday-ticket-creation

**Definition:** `.claude/skills/monday-ticket-creation/SKILL.md`

## What it does

Bulk-creates Monday.com board items from a written requirement source (an FR document, a plan, a spec section) — one item per requirement, grouped and prioritized to mirror the source document's own structure, with the full requirement text placed in each item's native Description field.

## How it does it

**Before touching the board**, it confirms scope rather than assuming it: which requirement source, if a document has more than one candidate list (e.g. Functional Requirements vs. a Detailed Plan's task breakdown — these produce different granularity/ordering); what "automation" means if the request uses that word, since Monday's own automations are trigger-based *inside* Monday (status change, form submit) and cannot watch an external document — the skill is explicit that a request for "automation so tickets get created based on X" almost always means a one-time bulk import, and says so rather than silently building something that can't do what was described, or silently building the import without mentioning the limitation; and which board (existing or new).

**Reads the board before writing to it** via `get_board_info`, since column IDs, status/dropdown label sets, and existing groups are per-board and must never be assumed or reused from memory of a different board.

**Structures the import to mirror the source document**: groups follow the document's own section structure in the same order (not an invented taxonomy); the board's default group gets renamed into the first real group (via `update_group`) rather than left as a stray empty default above the real groups; subsequent groups are created sequentially, each chained off the previous group's returned id (not parallelized, since each call depends on the prior result); items are named `<ReqID>: <short summary>`; priority is carried straight across from the document's own markers (Must→High, Should→Medium) without inventing a new scheme or silently downgrading a scoped-but-still-Must requirement; every new item defaults to the board's "not started" status label, since these are backlog items.

**Creates items in batches of up to 20** via `create_items`, with `columnValues` set for status/priority/type but the description deliberately left off this call.

**Sets the Description field separately**, via the same `set_item_description_content` GraphQL mutation `monday-quick-item` uses — batched manually with GraphQL aliases (since there's no dedicated batch tool for this mutation), 8–10 items per call to keep any single failure's blast radius small. The skill documents, in detail, two approaches that look like they should work and don't: adding a `long_text` column named "Description" (creates a real column, renders in the table area, not the native Description section) and using `create_doc` targeting a `direct_doc`-type column (errors outright — `create_doc` only accepts `doc`-type columns, and `direct_doc` is a different, incompatible type). If an earlier attempt already created a stray column while working this out, the skill's guidance is to leave it, tell the user it's there and redundant, and offer to remove it — not to silently delete it.

**Afterward**, it states plainly that this was a one-time snapshot — if the source document changes later, the board won't reflect that automatically, and re-running the import against the updated document is offered rather than implying anything stays in sync on its own.

## Why it exists

Turning a requirements/plan document into a populated Monday board is a recurring need on this project (the FR documents `aris-phase-documentation` produces are exactly the kind of source this skill consumes), and the process has several non-obvious failure points that are easy to get wrong every single time without a skill encoding the fix: the native-Description-field mechanism (documented in detail because it's "the part that's easy to get wrong"), the automation-vs-one-time-import distinction (a genuine, recurring source of miscommunication when a user says "set up automation" but means "import once"), and GraphQL-level constraints (every declared `$variable` must be referenced or the whole batched mutation call fails outright). Each of these was presumably learned the hard way once; the skill exists so it isn't relearned the hard way again.

## When it fires

Whenever asked to turn a requirements/plan document into Monday tickets, populate or seed a Monday board from project documents, or a request for "automation so tickets get created based on X" — which, per the skill's own guidance, should first be clarified rather than assumed to mean a live sync.

## How to invoke

- **Explicitly**: `/monday-ticket-creation`, or ask directly — "turn Phase 1's FR document into Monday tickets," "populate the board from this plan."
- **Implicitly**: the assistant should recognize a request to "turn a requirements/plan document into Monday tickets," "populate or seed a Monday board from project docs," or a vaguer "set up automation so tickets get created based on X" as this skill — that last phrasing specifically should trigger the skill's own automation-vs-one-time-import clarification step, not a silent assumption either way.

## Other details

- **Confirmation-first, unlike `monday-quick-item`** — bulk-populating a board (or a whole section of one) is a larger, more visible action than adding one item, so the skill requires confirming source/scope/board before creating anything, rather than proceeding directly.
- **Not ARIS-specific in its mechanics** — the Description-field workaround, the automation-vs-import clarification, and the GraphQL batching constraints all apply to any Monday board this skill is pointed at; only the "which document, which board" specifics are project-context.
- **The "don't silently delete a stray column" guidance** reflects a general principle applied here: a mistake made while exploring the right mechanism is left visible and explained rather than cleaned up invisibly, since a column that's quietly appeared or disappeared on a shared board is the kind of thing a teammate would otherwise have to guess the history of.
- **Its priority-mapping rule ("Must (as scoped)" stays Must-tier, never silently downgraded)** matters specifically because a requirement document's own priority markers are the source of truth for triage — the skill exists partly to prevent priority information from being lossy on the way into Monday.
