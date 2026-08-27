# phase-dependency-package-guard

**Script:** `.claude/hooks/phase-dependency-package-guard.sh`
**Event:** `PreToolUse`, matcher `Edit|Write`
**Enforcement:** hard block (`hookSpecificOutput.permissionDecision: "deny"`)

## What it does

Blocks an `Edit` or `Write` to a `.csproj` or `package.json` file the moment it introduces a reference to a package that belongs to a later phase's infrastructure — a message broker, a search-engine client, a vector-store client, or an LLM SDK — while the project is still in Phase 1.

Banned, in `.csproj` (`<PackageReference Include="...">`):
- `RabbitMQ.Client`, any `MassTransit*` package (message broker, Phase 2)
- `OpenSearch.Client`, `NEST` (search index, Phase 2)
- `Qdrant.Client` (vector store, Phase 4)
- `Anthropic.SDK`, `OpenAI` (LLM integration, Phase 4)

Banned, in `package.json` (dependency key):
- `amqplib`, `amqp-connection-manager`, `rabbitmq-client`
- `@opensearch-project/opensearch`, `@elastic/elasticsearch`
- `@qdrant/js-client-grpc`, `@qdrant/js-client-rest`
- `@anthropic-ai/sdk`, `openai`

## How it does it

1. Reads the tool-call JSON from stdin; exits allow (`{}`) immediately if `file_path` isn't a `.csproj` or `package.json`.
2. Pulls the *added* text — `tool_input.content` for a `Write` (the whole new file), `tool_input.new_string` for an `Edit` (just the inserted fragment). Either way, this is exactly the text this specific tool call is putting into the file.
3. Regex-matches that text against the banned list, scoped to look like a real package reference (`<PackageReference Include="...">` for .NET, `"<name>":` for npm) rather than a bare keyword — so a comment or unrelated string mentioning, say, "OpenAI" in passing doesn't trip it.
4. On a match, emits `{"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny", "permissionDecisionReason": "..."}}` — the edit never reaches disk.
5. On no match, emits `{}` and the edit proceeds normally (subject to whatever the user's own permission settings say).

## Why it exists

CLAUDE.md's non-negotiable list states: "Don't build ahead of the current phase... Each phase document lists explicit non-goals — respect them." Phase 1 Technical Documentation §11 names, explicitly, "Any message broker, outbox pattern, or event contracts (Phase 2)... Any search index (OpenSearch) or vector store (Qdrant) (Phase 2 / Phase 4)... LLM/agent integration of any kind (Phase 4)" as things that "should not be added under this phase's implementation."

This is a solo-developer project built phase-by-phase specifically so each layer (deterministic before generative, platform before clinical intelligence) gets focused attention. A stray `dotnet add package RabbitMQ.Client` — reached for out of habit, or because it seemed convenient for some unrelated problem — is exactly the kind of small, easy-to-miss step that quietly pulls Phase 2/4 infrastructure into a Phase 1 codebase. Catching it at the package-reference level, before any code is written against it, is cheaper than catching it in review after a dependency is already threaded through a service.

## When it fires

Every `Edit` or `Write` tool call targeting a `.csproj` or `package.json` file, anywhere in the repo — not scoped to a particular service, since any service could be the one where this slips in.

It does **not** fire on `.cs` source files, `docker-compose.yml`, or any other file type — a service could still reference RabbitMQ conceptually in a comment or design note without tripping this; it specifically targets the dependency declaration, which is the actual point of no return.

## Invocation

No explicit form — see the hooks README's "Invocation model" section for why. This one fires automatically, before the fact, on *every* `Edit`/`Write` whose target path ends in `.csproj` or `package.json`, regardless of what the assistant or user intended for that edit; it has no awareness of intent, only of file path and added text. The only way to bypass it for a single edit is to not match its file-path filter at all (e.g. editing a `.cs` file that references the same package name in a comment, which the hook's regex is specifically scoped to ignore).

## Other details

- **Blocks, doesn't warn** — deliberately. There's no legitimate reason to add one of these dependencies while Phase 1 is in effect; letting it through "to fix later" risks the dependency getting used before anyone notices.
- **This hook will need to be updated or removed once the project reaches the phase that actually needs one of these dependencies** (Phase 2 for the broker/search packages, Phase 4 for vector-store/LLM packages) — the block's own denial message says as much, so a future edit that's legitimately blocked carries a pointer to where to fix the rule, not just where to fix the edit.
- **False-positive surface**: low. The regex requires the exact package-reference syntax, not a bare keyword, so mentioning "RabbitMQ" in a code comment or markdown file won't trigger it (and markdown files aren't matched at all, since the hook only looks at `.csproj`/`package.json`).
- **Known gap**: doesn't catch a package added via `dotnet add package` run directly in a terminal (a `Bash` tool call rather than `Edit`/`Write`) — the resulting `.csproj` change would still be caught the next time that file is edited or written by Claude, but a manually-run CLI command that edits the file outside Claude's own `Edit`/`Write` tools bypasses this hook entirely, since the hook only fires on those two tool names.
