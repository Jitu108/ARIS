---
name: aris-phi-safe-log-audit
description: Audit logging, exception messages, and (once introduced) metrics/trace attributes for PHI-shaped fields leaking out of application code — the "PHI-safe logging from day one" non-negotiable. Use before finishing any change that touches an entity with identifying fields (Patient, User, and every clinical entity added in later phases), whenever asked to review/audit logging, or as the logging-specific check inside a broader code review.
---

# ARIS PHI-safe logging audit

Master spec §51 (PHI Protection): ARIS must avoid putting unnecessary PHI into application logs, exception messages, metrics, trace attributes, or debug output. Phase 1 Technical Documentation §5.4 makes this concrete: PHI-shaped fields are excluded from log messages by convention — **logs reference entity IDs, never identifying values.** This is called out in CLAUDE.md as a habit that starts in Phase 1, "not retrofitted later," because retrofitting it is expensive once real clinical data and years of log volume exist (Project Plan §6 cross-phase workstreams).

This is a static/convention audit, not a runtime DLP or redaction system — no scrubbing middleware or automated PHI-detection service exists or should be built ahead of a phase that calls for it. The mechanism that already exists is `BuildingBlocks`' PHI-safe logging helper (see [[aris-new-service-scaffold]] §1) — this skill checks that it's actually used, not that some other infrastructure needs building.

## 1. Current PHI-shaped field inventory

Keep this list in sync with the schema as new entities appear (§3 of each phase's Technical Documentation is the source). As of Phase 1:

| Entity (service) | PHI-shaped fields | Safe to log |
|---|---|---|
| `Patient` (PatientService) | `FirstName`, `LastName`, `DateOfBirth`, `Mrn` | `Id`, `SourceSystem`, entity-not-found/count facts |
| `User` (IdentityService) | `Username`, `Email`, `DisplayName` | `Id`, `IsActive`, `MustChangePassword`, role names |
| `AuthAuditEvent`, `RefreshToken`, `PasswordResetToken` | none directly (already ID/hash/timestamp-shaped) | everything — these tables were designed audit-safe from the start |

When Phase 2 introduces `Encounter`, `Diagnosis`, `Procedure`, `Note`, `Claim` (§21 of the master spec, deferred out of Phase 1 per §102), extend this table before those entities' logging is written — diagnosis/procedure codes and clinical note text are exactly the kind of field this convention exists to keep out of logs.

## 2. Where to look

For each service/PR under review, search for:

1. **Log call sites** — any call through the shared logging wrapper or a raw `ILogger`/`Console` call. Flag:
   - A message template that interpolates a PHI-shaped field directly (`$"Patient {patient.FirstName} {patient.LastName} not found"`).
   - Structured-logging destructuring of a whole entity (`logger.LogInformation("{@Patient}", patient)`) — this looks safe in the template string but serializes every property, PHI-shaped fields included, into the log sink. Flag destructuring of any entity that has PHI-shaped fields; destructuring a DTO containing only `Id`/status fields is fine.
   - A raw `ILogger` call bypassing the BuildingBlocks PHI-safe wrapper entirely in a service that has one available — that's a signal the convention was skipped, not just misapplied.
2. **Exception messages and `ToString()` overrides** — an exception thrown with a PHI-shaped value inlined (`throw new ValidationException($"Invalid DOB for {patient.LastName}")`) leaks it the moment the exception is logged or serialized into a problem-details `detail` field. Check both the throw site and any custom `ToString()`/`Message` override on domain exceptions.
3. **Problem-details responses** (§4.5) — confirm `detail`/`title` never carry a PHI-shaped value; these are the one thing that reaches the client directly, not just a log sink.
4. **Metrics and trace attributes** — not stood up in Phase 1 (§9: no distributed tracing backend yet), but once a phase introduces them, apply the same field inventory to tag/label values, not just log messages. This is explicitly named alongside logs in §51 — don't let it fall through when it eventually gets added.
5. **Debug/console output left in from development** — a stray `Console.WriteLine`/`print`-style line dumping a full entity is the easiest violation to introduce accidentally and the easiest to miss in review; grep for leftover debug prints touching any entity in the table above.

## 3. What a clean result looks like

- Every log line referencing a `Patient` or `User` (or later, clinical entity) does so by `Id` only, plus non-identifying context (status, counts, correlation ID).
- Every exception message is generic/entity-ID-based; anything more specific goes into a `detail` field that itself follows the same rule.
- No destructured whole-entity logging for any type in the PHI-shaped field inventory.
- The service actually uses the shared `BuildingBlocks` PHI-safe logging helper rather than a raw logger call — consistent usage is itself part of what this audit confirms, not just field-by-field correctness.

## 4. Reporting

When run as part of a review, report findings as concrete file:line + the offending log/exception statement + which PHI-shaped field leaked and how (message interpolation, structured destructuring, exception message, stray debug output) — not a general "logging looks fine" pass. A clean scan should say so explicitly and name what was checked (which files/entities), not just "no issues found."