#!/usr/bin/env bash
# PreToolUse (Edit|Write) — block Phase-2+ package references in .csproj/package.json
# while the project is still Phase 1. Grounded in CLAUDE.md phase discipline and
# Phase 1 Technical Documentation §11 Non-Goals.
set -euo pipefail

input="$(cat)"
tool_name="$(jq -r '.tool_name // empty' <<<"$input")"
file_path="$(jq -r '.tool_input.file_path // empty' <<<"$input")"

allow() { printf '{}'; exit 0; }

[[ -z "$file_path" ]] && allow

case "$file_path" in
  *.csproj|*package.json) ;;
  *) allow ;;
esac

if [[ "$tool_name" == "Write" ]]; then
  new_text="$(jq -r '.tool_input.content // empty' <<<"$input")"
else
  new_text="$(jq -r '.tool_input.new_string // empty' <<<"$input")"
fi

[[ -z "$new_text" ]] && allow

violation=""

if [[ "$file_path" == *.csproj ]]; then
  # .NET PackageReference — match on the Include= value so a comment/string elsewhere doesn't trip this.
  match="$(grep -Eio '<PackageReference[[:space:]]+Include="(RabbitMQ\.Client|MassTransit[^"]*|OpenSearch\.Client|NEST|Qdrant\.Client|Anthropic\.SDK|OpenAI)"' <<<"$new_text" | head -1 || true)"
  [[ -n "$match" ]] && violation="$match"
fi

if [[ "$file_path" == *package.json ]]; then
  match="$(grep -Eio '"(amqplib|amqp-connection-manager|rabbitmq-client|@opensearch-project/opensearch|@elastic/elasticsearch|@qdrant/js-client-grpc|@qdrant/js-client-rest|@anthropic-ai/sdk|openai)"[[:space:]]*:' <<<"$new_text" | head -1 || true)"
  [[ -n "$match" ]] && violation="$match"
fi

if [[ -n "$violation" ]]; then
  reason="Blocked: '$violation' is a Phase 2+ dependency (message broker / search index / vector store / LLM SDK). Phase 1 is synchronous-HTTP-only with no such infrastructure (CLAUDE.md; Phase 1 Technical Documentation §11 Non-Goals). If this project has since moved past Phase 1, update or remove this hook in .claude/settings.json."
  jq -n --arg reason "$reason" \
    '{hookSpecificOutput: {hookEventName: "PreToolUse", permissionDecision: "deny", permissionDecisionReason: $reason}}'
  exit 0
fi

allow
