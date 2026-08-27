---
name: monday-quick-item
description: Create a single Monday.com board item from just a title and a description — a quick-add, not a bulk import. Defaults to the ARIS "Tasks" board (id 5030912763), drops the item in that board's current top group, sets Status to its "not started" label, and writes the description into the item's native Description field. Use whenever asked to "add a ticket/item to Monday for X" with a one-off title + description, as opposed to importing many items from a requirements doc (that's `monday-ticket-creation`).
---

# Monday quick item

Single-item counterpart to `monday-ticket-creation`: takes a **title** and a **description**, creates exactly one item. No group/status/priority decisions needed from the caller — this skill picks sensible defaults itself.

## Inputs

- **Title** (required) — becomes the item's name verbatim. Don't invent an ID prefix or reformat it; the caller's title is the title.
- **Description** (required) — free text (can be multi-paragraph/markdown-ish). Goes into the item's native Description field, not a column.

If either is missing, ask for it rather than guessing content.

## Steps

1. **Resolve the board and its current shape** — call `get_board_info` for board `5030912763` (`filters.columns.only: true` is enough; you need `top_group.id` and the `task_status` column's label set). Don't hardcode a group ID from a previous session — the top group can change over time, so read it fresh each call.
2. **Create the item** with `create_items` (a single-item array is fine):
   - `groupId`: the board's current `top_group.id`.
   - `columnValues`: set `task_status` to the board's "not yet started" label (currently `"Ready to start"` — reconfirm against the labels returned in step 1 rather than assuming it hasn't changed) as `{"task_status": {"label": "Ready to start"}}`. Leave `task_priority` and `task_type` unset — those are for the caller to fill in later if this quick-add doesn't have enough signal to guess them; don't invent a priority.
   - Do **not** put the description text in `columnValues` here — see step 3.
3. **Set the description** via the native item-description mutation, called through `all_api_write` (no dedicated tool wraps this):
   ```graphql
   mutation ($itemId: ID!, $markdown: String!) {
     set_item_description_content(item_id: $itemId, markdown: $markdown) {
       block_ids
     }
   }
   ```
   Pass the description text as `markdown` verbatim (light markdown — bold/lists/paragraphs — is fine and renders). This is the field that shows under "Description" in the item detail view; it is **not** a `long_text` column and **not** a `doc`/`direct_doc` column — those look plausible but don't populate that section (see `monday-ticket-creation` for the full explanation of why, if this needs re-deriving).
4. **Report back** the created item's name and `item_url` from the `create_items` result — that's the confirmation the caller needs, no further summary required.

## Notes

- This is a single synchronous action with a small, easily-undone blast radius (one item, on a board already in active use) — no need to ask for confirmation before creating it, just do it and report the result.
- If the caller names a different board explicitly, use that board's id instead of the ARIS Tasks default, and re-derive its top group / status labels the same way — don't assume another board shares this board's column IDs or label text.
