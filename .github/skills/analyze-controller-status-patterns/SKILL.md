---
name: analyze-controller-status-patterns
description: 'Analyze controller status-setting patterns (e.g., this.setStatus), summarize usage, find duplication, and propose safe refactoring with before/after examples.'
argument-hint: Symbol or pattern to analyze (example: this.setStatus)
---

# Analyze Controller Status Patterns

## When to Use

- You need a usage map of `this.setStatus()` or similar status-setting APIs.
- You want to reduce repeated controller logic.
- You need concrete refactor recommendations and examples.

## Procedure

1. Search all source controllers for the target pattern.
2. Exclude documentation-only hits from analysis.
3. Group by status code and by feature area.
4. Identify repeated branches and duplicated private helpers.
5. Propose refactors that preserve controller thinness.
6. Provide before/after examples with improved error handling.

## Deliverables

- Where it is used (file-level summary)
- Repeated patterns in controllers
- Refactor suggestions to reduce duplication
- Before/after code examples
