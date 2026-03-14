---
name: self-improvement
description: "Self-improvement loop for learning from mistakes. Use when: receiving a correction from the user, noticing a repeated mistake, or at session start to review past lessons. Updates tasks/lessons.md with patterns that prevent recurring mistakes."
---

# Self-Improvement Loop

After ANY correction from the user: capture what went wrong and write a rule to prevent it.

## When to Use

- After receiving a correction or feedback from the user
- When you notice a repeated mistake pattern
- At session start — review existing lessons for the relevant project

## Procedure

### After a Correction

1. **Acknowledge** — Understand what went wrong
2. **Update `tasks/lessons.md`** — Add an entry with this pattern:

```markdown
## [Date] — [Brief description]

**Mistake:** What I did wrong
**Root cause:** Why it happened
**Rule:** Specific rule to prevent this in the future
**Project:** Which project/area this applies to
```

3. **Write specific prevention rules** — Not vague "be more careful", but concrete rules like "Always check if the interface has changed before implementing"
4. **Iterate** — Ruthlessly refine these rules until mistake rate drops

### At Session Start

1. **Review `tasks/lessons.md`** — Check for lessons relevant to the current task
2. **Apply proactively** — Use past lessons to avoid known pitfalls

## Rules

- Every correction gets a lesson entry — no exceptions
- Rules must be specific and actionable
- Review and update rules that turn out to be wrong or incomplete
- Delete rules that are no longer relevant
