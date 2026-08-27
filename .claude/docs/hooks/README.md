# ARIS Claude Code hooks

Five mechanical, deterministic gates wired into `.claude/settings.json`, backed by scripts in `.claude/hooks/`. These are pattern-matching checks that don't need model judgment — contrast with the project's skills (`aris-new-service-scaffold`, `aris-phi-safe-log-audit`, `aris-rbac-matrix-sync`, `aris-phase-documentation`) and agents (`auth-session-security-reviewer`, `fr-techdoc-testdoc-traceability-auditor`, `phase-scope-guard`), which handle judgment-based review a regex can't.

| Hook | Event | Enforcement | Doc |
|---|---|---|---|
| `phase-dependency-package-guard` | PreToolUse (Edit\|Write) | Hard block | [phase-dependency-package-guard.md](phase-dependency-package-guard.md) |
| `password-response-guard` | PreToolUse (Edit\|Write) | Hard block | [password-response-guard.md](password-response-guard.md) |
| `solution-structure-reference-lint` | PostToolUse (Edit\|Write) | Block (reported back to Claude) | [solution-structure-reference-lint.md](solution-structure-reference-lint.md) |
| `compose-healthcheck-lint` | PostToolUse (Edit\|Write) | Warn only | [compose-healthcheck-lint.md](compose-healthcheck-lint.md) |
| `service-db-isolation-check` | PostToolUse (Edit\|Write) | Warn only | [service-db-isolation-check.md](service-db-isolation-check.md) |

## Why PreToolUse vs. PostToolUse

The two hard-blocking rules run **PreToolUse** because they check a hard, unconditional project rule (no Phase-2+ dependency in Phase 1; never echo a password back) against the content of the edit itself (`tool_input.content`/`tool_input.new_string`) — no need to wait for the write to land, and blocking before the write means a bad edit never touches disk.

The three structural/advisory checks run **PostToolUse** because they need to read the *whole* resulting file from disk (a `.csproj`'s complete `<ItemGroup>`, a full `docker-compose.yml`, a full `appsettings.json`) to make sense of it — an `Edit`'s `new_string` alone is just the diff fragment, not enough context to check "does this file have a healthcheck anywhere" or "how many connection strings does this file declare in total."

## Why two are hard blocks and three are warnings

- **Hard blocks** (`phase-dependency-package-guard`, `password-response-guard`) enforce rules the project documentation states as absolute, with no legitimate exception path: Phase 1 has zero tolerance for Phase 2+ infrastructure (CLAUDE.md, Phase 1 Technical Documentation §11), and a response must *never* echo a password back under any circumstance (FR-6.15). There's no scenario where letting the edit through and fixing it later is preferable.
- **Warnings** (`solution-structure-reference-lint` is the one exception — see its own doc for why it blocks despite being structural) and the other two advisory checks (`compose-healthcheck-lint`, `service-db-isolation-check`) flag conditions that are often true *mid-edit* — a `docker-compose.yml` service block being written top-to-bottom won't have its `healthcheck:` key yet on the first save, and a multi-file scaffold might briefly have an unresolved cross-service reference before the next edit fixes it. Blocking on these would fight normal incremental editing; reporting them (via `systemMessage`) keeps them visible without being adversarial.

## Shared mechanics

Every hook:
- Reads the tool-call JSON from stdin (`tool_name`, `tool_input.file_path`, and either `.content` for `Write` or `.new_string` for `Edit`).
- Exits early with `{}` (a no-op JSON allow) the moment the file path or content doesn't match what it cares about — cheap and silent for the overwhelming majority of edits that have nothing to do with any given rule.
- Is a plain bash script using only `jq`, `grep`, `sed`, and `awk` — no other runtime assumed, since the project itself has no build tooling yet (docs-only repo at the time these were written).
- Was pipe-tested against synthesized stdin JSON for both its allow and block/warn paths before being wired into `settings.json`, and the wiring itself was proven to fire live (not just pass a dry-run) via a temporary sentinel-file trigger before being cleaned up.

## Invocation model — hooks have no "explicit" invocation

This is the key way hooks differ from skills and agents: **a hook is never invoked by name, by the user or by the assistant.** There's no `/phase-dependency-package-guard` command and no way to "ask for" a hook to run on demand — its only trigger is the `matcher`/event pair configured in `.claude/settings.json` (here, always `Edit|Write`). Every firing is implicit and automatic: the instant any `Edit` or `Write` tool call matches the hook's event and matcher, Claude Code runs the script and applies whatever it returns, with no judgment call from the assistant about whether this is "the right moment" — that's the entire point of a hook over a skill or agent for these five rules, per each doc's "why" section.

Two things can look like "explicit invocation" but aren't the same as the hook firing:
- **Running the script directly** (e.g. `bash .claude/hooks/phase-dependency-package-guard.sh` with synthesized stdin, as used during development/testing) invokes the *script*, not "the hook" — the hook is the wiring in `settings.json` that causes automatic firing on a real tool call; running the file by hand doesn't go through that wiring at all and won't block or warn on anything (there's no real `Edit`/`Write` for it to react to).
- **Asking the assistant to "check for X"** that a hook already covers (e.g. "make sure this docker-compose.yml has healthcheck blocks") will get answered by the assistant reasoning about the file directly, not by manually triggering `compose-healthcheck-lint.sh` — the hook will separately and automatically re-check the same file the next time it's actually written.

## Editing or disabling a hook

Each script is self-contained in `.claude/hooks/<name>.sh` — edit the regex/logic there directly, no need to touch `settings.json` unless changing the event, matcher, or which script runs. To disable one temporarily, comment out or remove its entry from the relevant `hooks.PreToolUse`/`hooks.PostToolUse` array in `.claude/settings.json` (or set `"disableAllHooks": true` to kill every hook in the project). After editing `.claude/settings.json` itself, run `/hooks` once if a change doesn't seem to take effect — the file watcher only picks up a settings file that existed when the session started.
