---
name: autonomous-bugfix
description: "Autonomous bug fixing workflow. Use when: given a bug report, failing test, error log, or CI failure. Just fix it — don't ask for hand-holding. Point at logs, errors, failing tests, then resolve them. Zero context switching required from the user."
---

# Autonomous Bug Fixing

When given a bug report: just fix it. Don't ask for hand-holding.

## When to Use

- Bug reports from users or team members
- Failing tests (local or CI)
- Error logs or stack traces
- CI/CD pipeline failures

## Procedure

1. **Identify the failure** — Read the error, log, or failing test output
2. **Locate the root cause** — Trace the error to its source. Find root causes, not symptoms
3. **Fix it** — Implement the fix directly. No temporary workarounds
4. **Prove it works** — Run the relevant tests to verify the fix
5. **Check for regressions** — Run related tests to ensure nothing else broke
6. **Report what you did** — Brief summary of root cause and fix

## Rules

- **Zero context switching** — The user should not need to provide more info if it's available in logs/tests
- **No hand-holding** — Don't ask "should I fix this?" — just fix it
- **Root causes only** — Find and fix the actual problem. No band-aids
- **Go fix failing CI tests** without being told how
- **Senior developer standards** — The fix should be what a staff engineer would approve
