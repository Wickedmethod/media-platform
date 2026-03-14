---
name: implement-from-plan
description: 'Implement code changes from Planner/Analyst output with minimal patches, architecture preservation, and clear file-by-file reasoning. Use when implementing planned work in existing codebases.'
argument-hint: Planner/Analyst output or requested change scope
---

# Implement From Plan

## When to Use

- You have a plan from Planner/Analyst and need to implement safely.
- You need incremental patches instead of full rewrites.
- You must preserve architecture, naming, and style.

## Procedure

1. Read relevant instructions and conventions for the workspace.
2. Inspect existing code structure before changing anything.
3. List affected files and why each file must change.
4. Explain what will change and why it is minimal.
5. Apply small patches only where required.
6. Run validation checks (errors/tests/build) and fix regressions.
7. Return output in this order:
   - Summary of change
   - Files affected
   - Code patch
   - Explanation

## Guardrails

- Do not invent APIs unless explicitly requested.
- Do not rewrite whole files unless there is no safe alternative.
- Keep functions focused and avoid unnecessary dependencies.
