---
name: bugfix-root-cause-minimal
description: 'Find root cause of a bug, explain why it happens, and implement the smallest safe fix with tests. Use for production bug triage and fixes.'
argument-hint: Bug symptom, failing test, or error message
---

# Bugfix Root Cause Minimal

## Outcome

Produce a fix that addresses root cause (not symptoms) with minimal code churn.

## Procedure

1. Reproduce the issue (tests/errors/logs).
2. Identify where behavior diverges from expected behavior.
3. Isolate root cause and explain why it occurs.
4. Propose the smallest viable fix.
5. Patch only necessary files.
6. Add or update tests to prevent regression.
7. Validate with targeted checks.

## Required Report Format

1. Root cause
2. Why bug occurs
3. Minimal fix
4. Verification performed
