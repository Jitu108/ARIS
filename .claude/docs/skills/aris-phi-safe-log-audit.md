# aris-phi-safe-log-audit

**Definition:** `.claude/skills/aris-phi-safe-log-audit/SKILL.md`

## What it does

Audits logging, exception messages, and (once introduced in a later phase) metrics/trace attributes for PHI-shaped fields leaking out of application code, against a concrete, kept-current inventory of which fields on which entities count as PHI-shaped.

## How it does it

**§1 maintains a PHI-shaped field inventory**, kept in sync with the schema as new entities appear: `Patient` (`FirstName`, `LastName`, `DateOfBirth`, `Mrn`), `User` (`Username`, `Email`, `DisplayName`), and an explicit note that `AuthAuditEvent`/`RefreshToken`/`PasswordResetToken` are already ID/hash/timestamp-shaped and safe by design. It flags in advance that Phase 2's `Encounter`/`Diagnosis`/`Procedure`/`Note`/`Claim` entities must extend this table before their logging is written.

**§2 lists exactly where to look**: log call sites (message-template interpolation of a PHI-shaped field directly; structured-logging *destructuring* of a whole entity — `logger.LogInformation("{@Patient}", patient)` — which looks safe in the template string but serializes every property into the sink; a raw `ILogger` call bypassing the shared PHI-safe wrapper entirely); exception messages and `ToString()` overrides; problem-details responses (`detail`/`title` fields, since those reach the client directly); metrics/trace attributes once a later phase introduces them; and stray debug/console output left in from development, called out as "the easiest violation to introduce accidentally and the easiest to miss in review."

**§3 defines what a clean result looks like**: every log line referencing a tracked entity does so by `Id` plus non-identifying context only; every exception message is generic; no destructured whole-entity logging; and the service actually uses the shared `BuildingBlocks` PHI-safe logging helper rather than a raw logger call — consistent *usage* of the helper is itself part of what the audit confirms, not just field-by-field correctness.

**§4 sets the reporting bar**: concrete file:line plus the offending statement plus which field leaked and how — never a general "logging looks fine" pass; a clean scan must say explicitly what was checked (which files/entities), not just "no issues found."

## Why it exists

Master spec §51 states PHI must be kept out of application logs, exception messages, metrics, trace attributes, and debug output. Phase 1 Technical Documentation §5.4 makes this concrete for this project specifically: logs reference entity IDs, never identifying values. CLAUDE.md frames this as a habit that must start in Phase 1, "not retrofitted later" — because retrofitting PHI-safe logging after years of log volume and real clinical data exist is expensive in a way that building the habit early isn't. This is explicitly a static/convention audit, not a call to build a runtime DLP or redaction system — the mechanism (`BuildingBlocks`' PHI-safe logging helper) already exists per `aris-new-service-scaffold`; this skill's job is checking that mechanism is actually used, not inventing new infrastructure.

## When it fires

Before finishing any change that touches an entity with identifying fields (`Patient`, `User`, and every clinical entity added in later phases); whenever asked to review or audit logging specifically; or as the logging-specific check inside a broader code review.

## How to invoke

- **Explicitly**: `/aris-phi-safe-log-audit`, or ask directly — "audit this service's logging for PHI leakage."
- **Implicitly**: per the skill's own description, the assistant should run this check on its own — without being asked — "before finishing any change that touches an entity with identifying fields," i.e. anywhere `Patient`, `User`, or a later clinical entity is read, logged, or passed into an exception message. This is the kind of skill that should fire silently as part of finishing the work, not only when a review is explicitly requested; a change to `PatientService` logging that never explicitly says "audit for PHI" should still trigger it if it touches a tracked entity.

## Other details

- **Its field inventory is a living table, not a one-time snapshot** — the skill explicitly instructs extending it before Phase 2's clinical entities' logging is written, meaning this skill needs a deliberate touch-up at that phase boundary rather than being assumed complete forever.
- **Distinct from, and complementary to, `auth-session-security-reviewer`** (an agent) — both may review the same IdentityService code, but this skill checks whether PHI-shaped fields leak into logs, while that agent checks token/session mechanics; neither substitutes for the other.
- **Narrower than a general PHI/security review** — it doesn't check encryption at rest/in transit, access control, or any of the other PHI-protection concerns master spec §51's surrounding sections cover; it's scoped specifically to the logging/exception/metrics surface.
- **The "destructured whole entity" failure mode is the one most likely to be missed by a naive reviewer** — a message template that looks entirely safe (`"Patient lookup: {@Patient}"`) can still leak every PHI-shaped field into a structured-logging sink, which is why the skill calls this out as its own bullet rather than folding it into general "log interpolation" guidance.
