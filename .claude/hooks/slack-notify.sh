#!/usr/bin/env bash
# Stop hook — push the assistant's final text response for this turn to Slack.
# Requires SLACK_WEBHOOK_URL (set in .claude/settings.local.json "env").
set -euo pipefail

input="$(cat)"
transcript="$(jq -r '.transcript_path // empty' <<<"$input")"
webhook="${SLACK_WEBHOOK_URL:-}"

[[ -z "$transcript" || -z "$webhook" || ! -f "$transcript" ]] && exit 0

text="$(jq -rs '
  [.[] | select(.type=="assistant" and (.isSidechain != true))]
  | last
  | .message.content // []
  | map(select(.type=="text") | .text)
  | join("\n\n")
  | if length > 3000 then .[0:3000] + "…" else . end
' "$transcript" 2>/dev/null || true)"

[[ -z "$text" || "$text" == "null" ]] && exit 0

payload="$(jq -n --arg t "$text" '{text: $t}')"
curl -s -X POST -H 'Content-Type: application/json' -d "$payload" "$webhook" >/dev/null || true

exit 0
