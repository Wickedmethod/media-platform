---
name: core-principles
description: "Core coding principles that always apply. Use when: writing any code, making any change, or reviewing work. Simplicity first, no laziness, minimal impact, subagent strategy."
applyTo: "**"
---

# Core Principles

These principles apply to ALL work in this workspace.

## Simplicity First

Make every change as simple as possible. Impact minimal code.

## No Laziness

Find root causes. No temporary fixes. Senior developer standards.

## Minimal Impact

Changes should only touch what's necessary. Avoid introducing bugs.

## Subagent Strategy

- Use subagents liberally to keep main context window clean
- Offload research, exploration, and parallel analysis to subagents
- For complex problems, throw more compute at it via subagents
- One task per subagent for focused execution
