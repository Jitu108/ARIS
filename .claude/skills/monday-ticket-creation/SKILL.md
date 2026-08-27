---
name: monday-ticket-creation
description: Bulk-create Monday.com board items (tickets) from a written requirement source (an FR doc, a plan, a spec section) — one item per requirement, grouped and prioritized to mirror the source doc's structure, with the full requirement text placed in the item's actual Description field. Use whenever asked to turn a requirements/plan document into Monday tickets, populate or seed a Monday board from project docs, or "set up automation so tickets get created based on X" (clarify first — see below, this is almost always a one-time import, not a live sync).
---

# Monday board ticket creation from a requirement source

Turns a structured requirement document (FR-x.x tables, a task breakdown, a plan's work-items) into Monday.com board items: one item per requirement, grouped to match the doc's sections, prioritized from the doc's own priority markers, with the full requirement text in the item's Description.

## Before touching the board

This is an external, visible action — confirm scope before creating anything. Ask (don't assume):

1. **Which requirement source** — if the doc has more than one candidate list (e.g. Functional Requirements vs. a Detailed Plan's task breakdown), they produce different granularity/ordering; ask which one, or both.
2. **What "automation" means** — a user asking for "automation so tickets get created based on requirement X" almost always wants a **one-time bulk import**, not a live sync. Monday.com automations are trigger-based *inside* Monday (status change, form submit, another board's event) — they cannot watch an external markdown/doc file. Say this explicitly and confirm one-time import is what's wanted; don't silently build a recipe that can't do what was asked, and don't silently skip mentioning the limitation either.
3. **Which board** — existing (get its URL/ID) or new.

## Read the board before writing to it

Always call `get_board_info` (`filters.columns.only: true`) first. Never guess column IDs, status labels, or whether a description field already exists — this board's columns, their exact IDs, and their label sets are per-board and must be read, not assumed. Note in particular:

- Which status/dropdown columns exist and their exact label strings (priority, type, status) — column values for status/dropdown columns must use `{"label": "<exact existing label>"}`; a label that doesn't already exist either needs `createLabelsIfMissing: true` or must be added first.
- Whether the board already has groups you should reuse vs. needing new ones.

## Structuring the import

- **Groups = the doc's own section structure.** If the doc has `### 3.1 Authentication`, `### 3.2 Authorization`, etc., those become Monday groups in the same order — don't invent a different taxonomy.
- **Rename the board's default group** into your first group name via `all_api_write` with the `update_group` mutation (`group_attribute: title`) rather than leaving a stray "All Tasks"/default-named empty group sitting above your real groups:
  ```graphql
  mutation ($boardId: ID!, $groupId: String!, $newValue: String!) {
    update_group(board_id: $boardId, group_id: $groupId, group_attribute: title, new_value: $newValue) { id }
  }
  ```
- Create the rest with `create_group`, chaining each with `relativeTo`/`positionRelativeMethod: "after_at"` off the previous group's returned `group_id` — these calls are sequential (each needs the prior result), not parallel.
- **Item naming:** `<ReqID>: <short summary>` (e.g. `FR-4.3: Explicit 'no results' state`) — keeps items scannable in list view without opening each one.
- **Priority mapping:** carry the doc's own priority marker straight across (e.g. Must → High, Should → Medium) rather than inventing a new scheme. If the doc scopes a requirement (e.g. "Must (as scoped)"), that's still Must-tier — don't downgrade it.
- **Status:** default every newly-imported item to the board's "not started yet" label (e.g. "Ready to start") — these are backlog items, not in-progress work.

## Creating items in bulk

Use `create_items`, up to 20 items per call — batch along your group boundaries where convenient so failures are easy to localize, but batch size is the only hard constraint (not group alignment). Set `columnValues` for status/priority/type columns as `{"label": "..."}` JSON per item; leave the description off this call (see below — it needs a different mechanism).

## The Description field — the part that's easy to get wrong

A Monday item's visible "Description" section (the one under the Info panel in the item detail view, with the "Write something or type `/`" placeholder) is **not** a regular column. Do not try to fill it by:
- Adding a `long_text` column and calling it "Description" — it becomes a real column with real data, but it renders in the item's *table/column* area, not in that native Description section. Confusing but real: this looks like a plausible fix and silently isn't one.
- `create_doc` with `location: "item"` targeting a `direct_doc`-type column (e.g. a pre-existing "monday Doc v2" column some board templates ship with) — this errors (`InvalidColumnTypeException: column type should be doc`); `create_doc` only accepts columns of type `doc`, and `direct_doc` is a different, incompatible type.

The actual mechanism: every item has a native `description` field (GraphQL type `ItemDescription`, its own doc-like block store, entirely separate from board columns) that starts `null` until set. Set it with the `set_item_description_content` mutation via `all_api_write` — it isn't exposed as a dedicated tool, so call it as raw GraphQL:

```graphql
mutation ($itemId: ID!, $markdown: String!) {
  set_item_description_content(item_id: $itemId, markdown: $markdown) {
    block_ids
  }
}
```

(Requesting `id` on the result errors — the return type is `DocBlocksFromMarkdownResult`, which only has `block_ids`.)

There's no batch tool for this mutation and no dedicated MCP tool wraps it, so batch it yourself with GraphQL aliases in one `all_api_write` call — 8–10 items per call is a reasonable size (keeps any single failure's blast radius small and avoids oversized request bodies):

```graphql
mutation ($i1: ID!, $m1: String!, $i2: ID!, $m2: String!, ...) {
  b1: set_item_description_content(item_id: $i1, markdown: $m1) { block_ids }
  b2: set_item_description_content(item_id: $i2, markdown: $m2) { block_ids }
  ...
}
```
Every declared `$var` must be referenced somewhere in the mutation body or the call fails outright (`Variable "$iN" is never used`) — don't declare items you then decide to skip in that batch.

Format each requirement's markdown consistently, e.g.:
```
**Actor:** <actor/role>
**Priority:** <Must/Should/...>

<the full requirement statement>

**Acceptance Criteria:** <the Given/When/Then or equivalent>
```

## If you created a stray column while figuring this out

If an earlier attempt added a `long_text`/`doc` column that turned out not to be the real Description field, don't silently delete it — tell the user it's there, redundant, and offer to remove it. It's harmless as a spare column but shouldn't linger without the user knowing why.

## Setting expectations afterward

State plainly that this was a one-time snapshot: if the source document changes later, the board won't reflect that automatically (per the automation-type clarification above) — offer to re-run the import against the updated doc rather than implying anything stays in sync on its own.
