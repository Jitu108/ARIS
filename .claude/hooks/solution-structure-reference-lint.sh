#!/usr/bin/env bash
# PostToolUse (Edit|Write) — enforce the fixed project-reference direction from
# Phase 1 Technical Documentation §1.3: Domain has zero <ProjectReference>;
# Application never references Infrastructure.
set -euo pipefail

input="$(cat)"
file_path="$(jq -r '.tool_input.file_path // .tool_response.filePath // empty' <<<"$input")"

pass() { printf '{}'; exit 0; }

[[ -z "$file_path" || ! -f "$file_path" ]] && pass
[[ "$file_path" == *.csproj ]] || pass

# Expect .../src/Services/<Service>/ARIS.<Service>.<Layer>/<name>.csproj
layer="$(grep -Eo 'ARIS\.[^./]+\.(Domain|Application|Infrastructure|Api)/' <<<"$file_path" | grep -Eo '(Domain|Application|Infrastructure|Api)' | head -1 || true)"
[[ -z "$layer" ]] && pass

content="$(cat "$file_path")"
refs="$(grep -Eo '<ProjectReference[^>]*Include="[^"]*"' <<<"$content" || true)"

violation=""

if [[ "$layer" == "Domain" && -n "$refs" ]]; then
  violation="Domain project '$file_path' has a <ProjectReference> ($refs) — §1.3 requires Domain to have zero project references."
fi

if [[ "$layer" == "Application" ]]; then
  bad_ref="$(grep -Eo '<ProjectReference[^>]*Include="[^"]*Infrastructure[^"]*"' <<<"$content" || true)"
  if [[ -n "$bad_ref" ]]; then
    violation="Application project '$file_path' references Infrastructure ($bad_ref) — §1.3: Application defines the interfaces Infrastructure implements, never the reverse."
  fi
fi

if [[ -n "$violation" ]]; then
  jq -n --arg reason "$violation" \
    '{decision: "block", reason: $reason, systemMessage: ("Solution structure violation: " + $reason)}'
  exit 0
fi

pass
