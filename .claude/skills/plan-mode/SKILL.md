---
name: plan-mode
description: "Enter plan mode for non-trivial tasks (3+ steps or architectural decisions). Use when: starting complex features, multi-step refactors, architectural changes, or when something goes sideways and you need to re-plan. Stops premature building and reduces ambiguity."
---

# Plan Mode Default

Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions).

## When to Use

- Starting a feature that touches multiple files or modules
- Architectural decisions or design changes
- Multi-step refactors
- When something goes sideways mid-implementation
- When verification steps are needed, not just building

## Procedure

1. **Stop and assess** — Before writing any code, determine if the task has 3+ steps or involves architectural decisions
2. **Write detailed specs** — Document what will change, why, and expected outcomes upfront to reduce ambiguity
3. **Write plan to `tasks/todo.md`** — Create checkable items for each step
4. **Use plan mode for verification steps** — Plan how you'll verify each change, not just how you'll build it
5. **Get confirmation** — Check in with the user before starting implementation

## Re-planning

If something goes sideways during implementation:

- **STOP immediately** — Don't keep pushing
- **Re-plan** — Reassess the approach with the new information
- **Update `tasks/todo.md`** — Revise the plan with the adjusted steps
- **Communicate** — Explain what went wrong and the new approach
