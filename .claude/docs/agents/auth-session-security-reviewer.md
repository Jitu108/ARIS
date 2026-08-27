# auth-session-security-reviewer

**Definition:** `.claude/agents/auth-session-security-reviewer.md`
**Tools:** Read, Grep, Glob, Bash
**Model:** inherit (whatever model the invoking session is using)

## What it does

Reviews a change touching authentication, JWT issuance/validation, refresh-token handling, session/token revocation, or the forced-password-change gate — in `IdentityService` or in any other backend service — against five specific mechanics the project's planning documents call out as high-risk:

1. Token signing and validation (RS256, independent per-service validation, claims not re-checked live from DB).
2. Refresh-token rotation and revocation (hash-only storage, rotation-on-use, reuse-triggers-chain-revocation, bulk revocation on deactivation/password-reset, no restoring old sessions on reactivation).
3. The forced-password-change gate as a *cross-service* concern (present in every backend service, not just IdentityService; correct allow-list; `MustChangePassword=1` set unconditionally on admin resets; password never echoed back).
4. Anti-enumeration (identical password-reset response regardless of account existence; identical 401 for a deactivated account vs. a wrong password).
5. Secrets handling (env/Docker secrets, not hardcoded; private signing key held only by the issuing service).

## How it does it

It's given `Read`, `Grep`, and `Glob` to actually read the diff/files in question (not reason abstractly), and `Bash` to run supporting commands if needed (e.g., grepping across multiple files for a pattern, or checking whether a particular endpoint's route exists anywhere in the codebase). Its system prompt lays out the five areas above as an explicit checklist and instructs it to:

- Work through each area and report *concretely* — file:line plus what's wrong, or file:line plus what proves it's correct — rather than an impressionistic pass/fail.
- Report a *failure scenario* for anything flagged: what input or call sequence breaks it, and what the visible symptom would be (a stale session staying valid, a user routing around the change-password gate, an enumeration oracle).
- Skip areas that don't apply to the diff at hand (e.g., no refresh-token code touched) rather than padding the report with irrelevant checks.
- Stay out of general security hygiene, code style, or unrelated bugs — that's explicitly out of scope, deferred to the built-in `security-review`/`code-review` skills.

## Why it exists

The Phase 1 Detailed Plan's own risk table names several of these exact failure modes as the most likely ways a solo developer gets this wrong: "Downstream services trusting Ocelot blindly," "Skipping the audit-event table because 'nothing to audit yet,'" "Treating deactivation as just an `IsActive` flag flip, forgetting session revocation," "Implementing the forced-change gate... only in IdentityService," and "Echoing the entered password back in the reset-password API response." These aren't generic security concerns a broad-spectrum reviewer would necessarily catch — they're specific to this project's token/session design (RS256 chosen specifically to keep an OIDC door open; refresh-token chain-revocation as the compromise signal; the forced-change gate applying identically across services). A reviewer needs to already know these mechanics are the ones to check, and check each one by name, rather than rediscovering them from first principles on every review.

This was selected over building it as a hook because every one of the five checks requires reading and reasoning about code — e.g., confirming "every backend service validates the JWT independently" means actually finding the JWT-validation configuration in *each* service and confirming it's really there, not matching a keyword.

## When it fires

Invoke it whenever a change touches:
- `POST /identity/login`, `/refresh`, `/logout`, `/me`, or anything in the token-issuance path.
- Any user-management endpoint that revokes sessions (deactivate, admin password reset, self-service password-reset confirm).
- The forced-password-change gate middleware, in IdentityService or in any *other* service that's just had JWT validation wired in (per `aris-new-service-scaffold`'s checklist) — this is exactly the moment the Detailed Plan's risk table warns the gate is likely to be forgotten.
- Any newly-scaffolded service's JWT bearer configuration, to confirm it's independent and not just trusting Ocelot's forwarded header.

## How to invoke

- **Explicitly**: ask by name or by clear intent — "run auth-session-security-reviewer on this," "review this refresh-token change for security correctness."
- **Implicitly**: this is the one agent in this project whose own description uses the word "proactively" — "Use proactively whenever a change touches authentication, JWT issuance/validation, refresh-token handling, session/token revocation, or the forced-password-change gate — in IdentityService or in any other backend service." That phrasing is a deliberate signal: the assistant should spawn this agent as a matter of course whenever a change lands in that territory, not wait to be asked. In practice, that means invoking it as a follow-up step after implementing (or reviewing) any of: login/refresh/logout, a session-revoking endpoint, or JWT-validation wiring newly added to a service per `aris-new-service-scaffold`.

## Other details

- **Read-only by design** (no `Edit`/`Write` in its tool list) — it reviews, it doesn't fix. A finding should be handed back to the main conversation to act on.
- **Doesn't require the code to exist yet in a runnable state** — it can review a scaffold-in-progress or a diff that hasn't been built/tested, since its checks are about what the code *says*, not runtime behavior.
- **Overlaps in file territory but not in concern with `aris-phi-safe-log-audit`** (a skill, not an agent) — both may look at IdentityService code, but this agent checks token/session mechanics while that skill checks whether logging leaks PHI-shaped fields. Neither substitutes for the other.
- **Will need to be revisited once Phase 6** introduces a managed Secrets Manager or an external OIDC provider (§94/§107) — the "signing key via env var, not a Secrets Manager" and "no external IdP yet" assumptions baked into its checklist are Phase 1–5 assumptions, not permanent ones.
