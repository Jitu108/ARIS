# solution-structure-reference-lint

**Script:** `.claude/hooks/solution-structure-reference-lint.sh`
**Event:** `PostToolUse`, matcher `Edit|Write`
**Enforcement:** block (`decision: "block"` + `systemMessage`, fed back to Claude as context)

## What it does

After a `.csproj` file under `src/Services/<Service>/ARIS.<Service>.<Layer>/` is created or edited, checks the fixed project-reference convention from Phase 1 Technical Documentation §1.3:

- A **Domain** project (`ARIS.<Service>.Domain.csproj`) must have **zero** `<ProjectReference>` elements at all.
- An **Application** project (`ARIS.<Service>.Application.csproj`) must **never** reference an `Infrastructure` project.

## How it does it

1. Reads the tool-call JSON; resolves the written file's path from `tool_input.file_path` (falling back to `tool_response.filePath`).
2. Exits (no-op) unless the path is a `.csproj` file whose path contains `ARIS.<something>.<Layer>/` for one of the four known layer names.
3. Since this runs `PostToolUse`, the file already exists on disk with its final content — the script reads the **whole file**, not just the diff, which is what lets it check "does this file have *any* `<ProjectReference>`" rather than only what one edit added.
4. Extracts every `<ProjectReference ... Include="...">` line via regex.
5. If the layer is `Domain` and any reference exists → violation.
6. If the layer is `Application` and any reference's `Include=` path contains `Infrastructure` → violation.
7. On a violation, emits `{"decision": "block", "reason": "...", "systemMessage": "..."}` — naming the exact file and the exact offending `<ProjectReference>` line.
8. On no violation, emits `{}`.

## Why it exists

This is the newest, most precise convention in the project — Phase 1 Technical Documentation §1.3 was added specifically to fix the .NET folder/project-reference structure after comparing several alternatives (see the Technical Documentation's own text: "This is the fixed convention every backend service follows... Once that convention is set, every subsequent service mirrors it exactly"). `aris-new-service-scaffold` (the skill) documents the same convention for a human/model to follow deliberately, but a skill only guides a scaffold that's built freshly with the skill in mind — it doesn't catch a later, unrelated edit that quietly adds a reference violating the rule (e.g., an IDE "add project reference" action, or a copy-pasted `<ItemGroup>` from a different project template). A convention this new and this precise is exactly the kind of thing that drifts without a mechanical backstop, because violating it doesn't look wrong at a glance — a `<ProjectReference>` in a `Domain.csproj` still compiles fine; it's only wrong relative to this project's specific layering discipline.

## When it fires

Any `Edit` or `Write` whose resulting file is a `.csproj` located under `src/Services/*/ARIS.*.{Domain,Application,Infrastructure,Api}/`. It does not fire on `Infrastructure` or `Api` project files at all — those layers are *allowed* to reference other layers (per §1.3's `Api → Infrastructure → Application → Domain` direction), so there's nothing to check there.

## Invocation

No explicit form — see the hooks README's "Invocation model" section. Fires automatically, after the fact, on every `Edit`/`Write` that lands a `.csproj` under a recognized `ARIS.<Service>.<Layer>/` path — the assistant learns about a violation only via the `systemMessage`/`reason` fed back after the write already happened, the same way it would learn about any other tool result. There's no way to suppress it for one intentional exception (e.g. a genuinely temporary mid-refactor state) other than completing the fix in the very next edit to that file.

## Other details

- **Blocks, unlike the other two `PostToolUse` checks** (`compose-healthcheck-lint`, `service-db-isolation-check`), which only warn. The distinction: a `.csproj`'s `<ProjectReference>` list is typically written in one shot (a project file isn't usually built up incrementally line-by-line the way a `docker-compose.yml` service block might be), so there's less risk of this firing on legitimately-incomplete mid-edit state. And because the rule is structural and binary (either the reference exists or it doesn't) rather than something that's naturally true only "eventually" during a multi-step build-out, blocking immediately is more useful than a warning that could be ignored across several more edits.
- **`decision: "block"` on `PostToolUse` doesn't undo the file write** — the edit already landed on disk by the time this hook runs (that's inherent to `PostToolUse`). What it does is feed the violation back to Claude as context so it can be fixed in a follow-up edit, and show the user a `systemMessage`. If a truly pre-write block is ever needed for this rule, it would have to move to `PreToolUse` and reconstruct the *resulting* file content in-script (from `old_string`/`new_string` or `content`) rather than reading it from disk — more complex, and not judged necessary here since a `.csproj` reference is easy to correct after the fact and unlikely to be built upon before the next tool call.
- **Only checks `Domain` and `Application`** — `Infrastructure` and `Api` projects have no reference restriction to check under §1.3 (they're expected to reference downward through the stack), so the script silently passes any edit to those layers.
- **Path-pattern dependent**: relies on the exact `ARIS.<Service>.<Layer>/` folder-naming convention holding. If a service were scaffolded with a different naming pattern (violating §1.3 in the first place), this hook wouldn't recognize its files as being subject to the rule at all — it can only check conformance to a pattern it can recognize, not invent the pattern from an unfamiliar structure.