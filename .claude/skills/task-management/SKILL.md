---
name: task-management
description: "Structured task management workflow. Use when: starting a new feature, multi-step task, or any work that needs tracking. Plan first to tasks/todo.md, verify plan, track progress, explain changes, document results, and capture lessons."
---

# Task Management

Structured workflow for tracking progress on any non-trivial task.

## When to Use

- Starting any feature or multi-step task
- When the user provides multiple requirements
- Any work that benefits from checkpointing

## Workflow

### 1. Plan First

Write plan to `tasks/todo.md` with checkable items:

```markdown
# [Task Name]

## Plan
- [ ] Step 1: Description
- [ ] Step 2: Description
- [ ] Step 3: Description

## Notes
- Key decisions or constraints
```

### 2. Verify Plan

Check in with the user before starting implementation. Confirm the plan makes sense and nothing is missing.

### 3. Track Progress

Mark items complete as you go:

```markdown
- [x] Step 1: Description ✅
- [ ] Step 2: Description ← in progress
- [ ] Step 3: Description
```

### 4. Explain Changes

Provide a high-level summary at each step — what changed, why, and what's next.

### 5. Document Results

Add a review section to `tasks/todo.md`:

```markdown
## Review
- All tests pass
- Changes verified against requirements
- No regressions found
```

### 6. Capture Lessons

Update `tasks/lessons.md` after corrections (see self-improvement skill).
