# password-response-guard

**Script:** `.claude/hooks/password-response-guard.sh`
**Event:** `PreToolUse`, matcher `Edit|Write`
**Enforcement:** hard block (`hookSpecificOutput.permissionDecision: "deny"`)

## What it does

Blocks an `Edit` or `Write` to a response/DTO-shaped C# file (`*Response.cs`, or `*Dto.cs` not also named like a request) the moment the added text declares a password-shaped member — `Password`, `NewPassword`, `PasswordHash`, `ConfirmPassword`, or anything else containing "password" on a line that also looks like a member declaration (`public ...`).

## How it does it

1. Reads the tool-call JSON; extracts `file_path` and the base filename.
2. Filters by filename:
   - `*Response.cs` → always checked.
   - `*Dto.cs` → checked, **unless** the filename also contains `Request` (e.g. `LoginRequestDto.cs`) — those legitimately carry a password field and are explicitly exempted.
   - Anything else → allowed immediately, no further check.
3. Pulls the added text (`content` for `Write`, `new_string` for `Edit`) — the same "just what this call is adding" logic as the other hooks.
4. Greps that text, case-insensitively, for a line containing both `public` and `password` — a heuristic for "this line declares a public member with a password-shaped name," without needing a real C# parser.
5. On a match, denies the tool call with a reason naming the exact offending line, and a note that a genuinely request-shaped type should be renamed away from `*Response.cs`/`*Dto.cs` (or the hook's own naming heuristic adjusted) if this is a false positive.
6. On no match, allows.

## Why it exists

FR-6.15 (Phase 1 Functional Requirements): the new/confirm password match is client-side only, and the password is **never echoed back** in any API response. Phase 1 Detailed Plan §13's risk table names this directly: "Echoing the entered password back in the reset-password API response (e.g., for a UI confirmation toast) — Defeats FR-6.15... The response body never includes the password field." Every one of the password-related endpoints (`POST /identity/users/{id}/reset-password`, `POST /identity/change-password`, `POST /identity/password-reset/confirm`) is specified in the Technical Documentation as deliberately *not* echoing the password back, precisely because an Administrator-set password is, by construction, known to a second person — the whole point of the forced-change gate is to close that exposure, and a response that echoes the password back defeats it regardless of what the gate does.

This is a mechanical, "should never happen" rule with no legitimate exception on the response side — unlike a *request* body, which necessarily carries the password the user just typed.

## When it fires

Any `Edit` or `Write` whose target file is named `*Response.cs` or `*Dto.cs` (and isn't also named like a request), anywhere in the repo — this isn't scoped to `IdentityService`, since any service could in principle define a user-summary or account-related response type.

## Invocation

No explicit form — see the hooks README's "Invocation model" section. Fires automatically, before the fact, on every `Edit`/`Write` whose target filename matches `*Response.cs` or `*Dto.cs` (and isn't also named like a request) — the assistant doesn't choose to run this check, it simply can't complete a matching write that trips the regex. The only way around it for a legitimate false positive is the naming fix the denial message itself suggests (rename away from the `Response`/`Dto` pattern, or have the naming heuristic in the script adjusted) — not a way to ask the hook to skip this one time.

## Other details

- **Naming-convention dependent.** The whole guard rests on `*Response.cs`/`*Dto.cs`/`*Request*` naming being followed consistently — if a response type is named something that doesn't match this pattern (e.g. `UserSummary.cs` with no `Response`/`Dto` suffix), this hook won't see it at all. This is a known, accepted gap: catching every possible C# type name would require actually parsing the class's role (is it returned from a controller action?), which is well past what a regex-based hook should attempt — that level of judgment belongs to `auth-session-security-reviewer` (see `.claude/agents/auth-session-security-reviewer.md`), which explicitly checks "the response body of any password-reset/change endpoint never echoes the new password back" as part of a broader review.
- **False-positive path exists and is intentional**: a genuinely request-shaped type that happens to be named `*Dto.cs` without `Request` in the name (e.g. `LoginDto.cs` instead of `LoginRequestDto.cs`) would be incorrectly blocked. The block's own denial message tells the developer exactly how to resolve this — rename the type, or adjust the script — rather than silently guessing which case applies.
- **Only checks the word "password"**, not more specific downstream concerns like whether a password *hash* (as opposed to plaintext) would be acceptable to expose — per FR-6.15 and the risk table, neither should ever appear in a response, so no such distinction is drawn.
- **Blocks, doesn't warn** — same reasoning as `phase-dependency-package-guard`: there's no scenario where letting a password field into a response DTO and fixing it "later" is preferable to catching it before the edit lands.
