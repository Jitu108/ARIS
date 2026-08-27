#!/usr/bin/env bash
# PostToolUse (Edit|Write) — advisory check on docker-compose.yml: every service
# should have a healthcheck, and depends_on should use the service_healthy
# condition form, not a bare list. Phase 1 Technical Documentation §7.1.
# Warn-only (non-blocking): compose files are often edited incrementally.
set -euo pipefail

input="$(cat)"
file_path="$(jq -r '.tool_input.file_path // .tool_response.filePath // empty' <<<"$input")"

pass() { printf '{}'; exit 0; }

[[ -z "$file_path" || ! -f "$file_path" ]] && pass
case "$(basename "$file_path")" in
  docker-compose.yml|docker-compose.yaml) ;;
  *) pass ;;
esac

issues="$(awk '
  BEGIN { in_services=0; svc=""; svc_indent=-1; has_hc=0; has_bad_depends=0 }
  function report(name, hc, bad_dep) {
    if (name == "") return
    if (!hc) print "service \"" name "\" has no healthcheck:"
    if (bad_dep) print "service \"" name "\" uses a bare depends_on list instead of condition: service_healthy"
  }
  /^services:[[:space:]]*$/ { in_services=1; next }
  in_services && /^[^[:space:]]/ { report(svc, has_hc, has_bad_depends); in_services=0 }
  in_services {
    if (match($0, /^  [A-Za-z0-9_-]+:[[:space:]]*$/)) {
      report(svc, has_hc, has_bad_depends)
      svc = $0; gsub(/^  /, "", svc); gsub(/:.*/, "", svc)
      has_hc=0; has_bad_depends=0
      next
    }
    if (svc != "") {
      if ($0 ~ /^[[:space:]]+healthcheck:/) has_hc=1
      if ($0 ~ /^[[:space:]]+depends_on:[[:space:]]*$/) { in_depends=1; depends_is_list=0; next }
      if (in_depends) {
        if ($0 ~ /^[[:space:]]+-[[:space:]]/) { depends_is_list=1 }
        else if ($0 ~ /condition:[[:space:]]*service_healthy/) { depends_is_list=0 }
        else if ($0 ~ /^[[:space:]]{2,}[A-Za-z]/ && $0 !~ /^[[:space:]]{6,}/) { in_depends=0 }
        if (depends_is_list) has_bad_depends=1
      }
    }
  }
  END { report(svc, has_hc, has_bad_depends) }
' "$file_path")"

if [[ -n "$issues" ]]; then
  msg="docker-compose.yml healthcheck lint (§7.1): $(tr '\n' '; ' <<<"$issues")"
  jq -n --arg m "$msg" '{systemMessage: $m}'
  exit 0
fi

pass
