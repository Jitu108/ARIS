#!/usr/bin/env bash
# PostToolUse (Edit|Write) — advisory check: a service shouldn't hold more than
# one connection string, or reference another service's Domain/Application/
# Infrastructure project. §111 "no service reaches into another service's
# database, ever." Warn-only.
set -euo pipefail

input="$(cat)"
file_path="$(jq -r '.tool_input.file_path // .tool_response.filePath // empty' <<<"$input")"

pass() { printf '{}'; exit 0; }

[[ -z "$file_path" || ! -f "$file_path" ]] && pass

owning_service="$(grep -Eo 'src/Services/[^/]+/' <<<"$file_path" | head -1 | sed -E 's#src/Services/##; s#/##')"
[[ -z "$owning_service" ]] && pass

issues=()

case "$file_path" in
  *appsettings*.json)
    count="$(jq '(.ConnectionStrings // {}) | length' "$file_path" 2>/dev/null || echo 0)"
    if [[ "$count" -gt 1 ]]; then
      issues+=("$(basename "$file_path") declares $count connection strings — a service should own exactly one database (§111)")
    fi
    ;;
  *.csproj)
    while IFS= read -r ref; do
      [[ -z "$ref" ]] && continue
      # ProjectReference paths are relative (e.g. ../../../Services/Foo/...), so match
      # on "Services/<name>/" without requiring the "src/" prefix the absolute file_path has.
      other_service="$(grep -Eo 'Services/[^/]+/' <<<"$ref" | head -1 | sed -E 's#Services/##; s#/##' || true)"
      if [[ -n "$other_service" && "$other_service" != "$owning_service" ]]; then
        issues+=("$(basename "$file_path") (owned by $owning_service) references a project under $other_service: $ref")
      fi
    done < <(grep -Eo '<ProjectReference[^>]*Include="[^"]*"' "$file_path" | grep -Eo 'Include="[^"]*"' | sed -E 's/Include="([^"]*)"/\1/')
    ;;
  *) pass ;;
esac

if [[ "${#issues[@]}" -gt 0 ]]; then
  msg="Service DB/reference isolation check: $(printf '%s; ' "${issues[@]}")"
  jq -n --arg m "$msg" '{systemMessage: $m}'
  exit 0
fi

pass
