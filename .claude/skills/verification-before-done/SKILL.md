---
name: verification-before-done
description: "Verification-before-done workflow. Use when: about to mark a task as complete, finishing a feature implementation, or presenting work as done. Never mark a task complete without proving it works. Run tests, check logs, diff behavior."
---

# Verification Before Done

Never mark a task complete without proving it works.

## When to Use

- Before marking any task as complete
- Before presenting work as "done" to the user
- After implementing a feature or fix
- Before creating a PR or committing

## Procedure

1. **Run tests** — Execute relevant unit tests, integration tests
2. **Check logs** — Look for errors, warnings, or unexpected behavior
3. **Diff behavior** — Compare behavior between main and your changes when relevant
4. **Staff engineer test** — Ask yourself: "Would a staff engineer approve this?"
5. **Demonstrate correctness** — Show concrete evidence that it works (test output, log excerpts, before/after)

## Checklist

- [ ] All relevant tests pass
- [ ] No new warnings or errors in logs
- [ ] Behavior matches the requirements
- [ ] Edge cases considered
- [ ] No regressions in related functionality
- [ ] A senior engineer would approve this

## Rules

- **Never assume it works** — Prove it
- **Tests are mandatory** — If there are tests, run them
- **Show evidence** — Don't just say "it works", show the output
- If you can't verify (no tests, no way to run), explicitly state what couldn't be verified
