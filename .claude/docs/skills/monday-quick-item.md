# monday-quick-item

**Definition:** `.claude/skills/monday-quick-item/SKILL.md`

## What it does

Creates a single Monday.com board item from just a title and a description — a quick-add, not a bulk import. Defaults to the ARIS "Tasks" board (id `5030912763`), drops the item in that board's current top group, sets its status to the "not started" label, and writes the description into the item's native Description field (not a column).

## How it does it

1. **Resolve the board's current shape** via `get_board_info` — specifically the current `top_group.id` and the `task_status` column's label set, read fresh every call rather than reused from a prior session, since the top group and label text can change over time.
2. **Create the item** with `create_items`: `groupId` set to the board's current top group, `task_status` set to its "not yet started" label (currently `"Ready to start"`, but reconfirmed against the labels just read rather than assumed). `task_priority` and `task_type` are deliberately left unset — the skill's position is that a quick-add usually doesn't have enough signal to guess those, so it doesn't invent a value.
3. **Set the description** through the native item-description mutation (`set_item_description_content`), called as raw GraphQL via `all_api_write` — there's no dedicated tool wrapping it. The description is *not* placed in `columnValues` at item-creation time; that would either fail or land in the wrong place (see "why" below).
4. **Report back** the created item's name and `item_url` — no further summary needed.

If either the title or description is missing, the skill asks rather than guessing content.

## Why it exists

It's the single-item counterpart to `monday-ticket-creation`, split out specifically so a one-off "add a ticket for X" request doesn't have to go through that skill's heavier machinery (board-scope confirmation, group-structuring decisions, batched bulk creation) when there's exactly one item and no structural decisions to make. The description-field mechanics it encodes exist because Monday's native item-description field is easy to get wrong: it looks like it should be a regular column (a `long_text` column named "Description," or a `doc`-type column) but neither actually populates the section that shows under the item's Info panel — only the `set_item_description_content` mutation does, and it isn't exposed as a dedicated MCP tool, so it has to be called as raw GraphQL. This skill exists partly just to encode that non-obvious mechanism so it isn't rediscovered (or gotten wrong) on every single-item request.

## When it fires

Whenever asked to "add a ticket/item to Monday for X" with a one-off title + description — as opposed to importing many items from a requirements document, which is `monday-ticket-creation`'s job instead.

## How to invoke

- **Explicitly**: `/monday-quick-item`, or ask directly — "add a Monday ticket for X."
- **Implicitly**: the assistant should recognize a one-off "add a ticket/item to Monday for X" request as this skill specifically — as opposed to `monday-ticket-creation` — based on scale alone (a single title + description, no source document to import from). A request naming a source document ("turn this FR section into tickets") should route to `monday-ticket-creation` instead, even without the user knowing the two skills exist separately.

## Other details

- **No confirmation needed before creating** — the skill explicitly treats this as a small, easily-undone action (one item, on a board already in active use), unlike `monday-ticket-creation`'s bulk import, which does require confirming scope first given its larger blast radius.
- **Board defaults are ARIS-specific**; if the caller names a different board, the skill re-derives that board's own top group and label text the same way, rather than assuming another board shares this board's column IDs or label strings.
- **Doesn't set priority or type** — this is a deliberate omission, not an oversight; those are left for the caller to fill in later once there's enough signal to set them meaningfully.
- **Shares its trickiest mechanism (the description field) with `monday-ticket-creation`**, which documents the full explanation of why the obvious-looking approaches (a `long_text` column, a `doc`-type column) don't work, in case that reasoning needs re-deriving.
