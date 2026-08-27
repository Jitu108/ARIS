#!/usr/bin/env bash
# PreToolUse (Edit|Write) — block a password-shaped property landing in a
# response/DTO-shaped C# file. FR-6.15: a password is never echoed back.
set -euo pipefail

input="$(cat)"
tool_name="$(jq -r '.tool_name // empty' <<<"$input")"
file_path="$(jq -r '.tool_input.file_path // empty' <<<"$input")"

allow() { printf '{}'; exit 0; }

[[ -z "$file_path" ]] && allow

base="$(basename "$file_path")"
# Only response-shaped files — request-shaped files legitimately carry a password field.
case "$base" in
  *Response.cs) ;;
  *Dto.cs) case "$base" in *Request*) allow ;; esac ;;
  *) allow ;;
esac

if [[ "$tool_name" == "Write" ]]; then
  new_text="$(jq -r '.tool_input.content // empty' <<<"$input")"
else
  new_text="$(jq -r '.tool_input.new_string // empty' <<<"$input")"
fi

[[ -z "$new_text" ]] && allow

# Look for a password-shaped identifier on a line that reads like a member declaration
# (has "public" on it) — cuts down on flagging comments/unrelated string literals.
hit="$(grep -Ei 'public.*password' <<<"$new_text" | head -1 || true)"

if [[ -n "$hit" ]]; then
  reason="Blocked: '$base' looks like a response/DTO type but this edit adds a password-shaped member ('$hit'). FR-6.15 / Detailed Plan §13: a response must never echo a password back, in any form. If this is genuinely a request-shaped type, rename it away from *Response.cs/*Dto.cs, or adjust .claude/hooks/password-response-guard.sh's naming heuristic."
  jq -n --arg reason "$reason" \
    '{hookSpecificOutput: {hookEventName: "PreToolUse", permissionDecision: "deny", permissionDecisionReason: $reason}}'
  exit 0
fi

allow
