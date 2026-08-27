# compose-healthcheck-lint

**Script:** `.claude/hooks/compose-healthcheck-lint.sh`
**Event:** `PostToolUse`, matcher `Edit|Write`
**Enforcement:** warn only (`systemMessage`, non-blocking)

## What it does

After `docker-compose.yml` (or `docker-compose.yaml`) is created or edited, walks every top-level service block under `services:` and reports, per service:

- Whether it has a `healthcheck:` key at all.
- Whether its `depends_on:` (if present) uses the mapping form with `condition: service_healthy`, rather than the bare list form (`depends_on: [- other-service]`).

## How it does it

1. Reads the tool-call JSON; resolves the file path; exits (no-op) unless the file's basename is `docker-compose.yml`/`docker-compose.yaml`.
2. Since this is `PostToolUse`, reads the **whole file** from disk (needed — a service's `healthcheck:` key could be many lines away from whatever line an `Edit` actually touched).
3. Runs a small `awk` state machine over the file:
   - Tracks when it's inside the top-level `services:` block (exits on any unindented top-level key).
   - Detects each direct child service (a 2-space-indented `<name>:` line) as the start of a new service block.
   - Within a service block, sets a flag if a `healthcheck:` line appears at any depth.
   - Within a service block, if a `depends_on:` line appears, watches the following lines: a `- <name>` line means the bare list form (flag it); a `condition: service_healthy` line means the mapping form (fine).
   - On leaving a service block (next sibling service, or end of file), reports any issues found for that service.
4. Collects all issues across all services into one `systemMessage` string; emits `{}` if there are none.

## Why it exists

Phase 1 Technical Documentation §7.1: "Compose `healthcheck:` blocks target each service's `/health/ready`; dependent services use `depends_on: condition: service_healthy` rather than a fixed startup delay." This isn't a stylistic preference — CLAUDE.md's Docker-first principle states plainly that "every slice must be runnable via `docker compose up`... this is the actual exit-criteria bar, not IDE-only verification," and a service that starts before its dependency (SQL Server, most obviously) is actually ready is a classic source of flaky, hard-to-reproduce `docker compose up` failures that look like application bugs but are really startup-ordering bugs. Catching a missing healthcheck or a bare `depends_on` the moment the compose file changes is cheaper than debugging an intermittent container-startup race later.

## When it fires

Any `Edit` or `Write` to a file named `docker-compose.yml` or `docker-compose.yaml`, anywhere in the repo (in practice, there's exactly one such file at the project root per Phase 1's topology).

## Invocation

No explicit form — see the hooks README's "Invocation model" section. Fires automatically, after the fact, on every `Edit`/`Write` to `docker-compose.yml`/`docker-compose.yaml`, no matter how incomplete that particular save is expected to be. Because this one only warns (`systemMessage`, non-blocking), "invocation" here just means the message appears in the transcript for the assistant to notice and act on if it chooses — there's no separate step to explicitly request or dismiss the check.

## Other details

- **Warns, never blocks.** A `docker-compose.yml` service block is often built up incrementally — the service name and `build:`/`image:` line first, `healthcheck:` and `depends_on:` added in a subsequent edit. Blocking on the first, incomplete save would be adversarial to normal editing; a `systemMessage` keeps the gap visible without forcing a specific edit order.
- **The `awk` parser is a heuristic, not a real YAML parser.** It assumes fairly conventional 2-space indentation and doesn't handle every valid YAML shape (flow-style mappings, anchors/aliases, a `depends_on:` written as an inline flow list `[a, b]` rather than a block list). For this project's actual compose file (a small, hand-written Phase 1 topology), that's an acceptable trade-off against the alternative of requiring a YAML-parsing dependency (`yq`, Python+PyYAML) the project doesn't otherwise need. If the compose file grows more complex or adopts different YAML conventions, this script should be revisited rather than trusted blindly.
- **Doesn't check *which* services need a healthcheck** — it flags every service missing one, including ones like `aris-web` (the Angular frontend) that nothing else depends on and that the Technical Documentation doesn't explicitly require a healthcheck for. This is a deliberate over-flag: a human (or Claude) reviewing the warning decides whether a given service's missing healthcheck is actually a gap or a non-issue, rather than the script guessing which services are "backend" and which aren't.
- **Doesn't validate the healthcheck's *content*** (e.g., that it actually targets `/health/ready` as §7.1 specifies) — only that the key exists. Verifying the check target is correct is left to manual review or a future enhancement, not this hook.