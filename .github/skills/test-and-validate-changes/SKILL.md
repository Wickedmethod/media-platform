---
name: test-and-validate-changes
description: 'Run focused validation for code changes: compile/lint/errors/tests, then report what passed, failed, and what remains risky.'
argument-hint: Changed files or target module
---

# Test And Validate Changes

## Goal

Ensure code changes are verified with the smallest practical validation scope first, then broader checks if needed.

## Procedure

1. Start with changed-file diagnostics.
2. Run targeted tests for impacted modules.
3. Run broader suite only if required.
4. Capture failures and map them to changed code.
5. Re-test after each fix.

## Output

- Checks run
- Results
- Remaining risks
- Recommended next validation step
