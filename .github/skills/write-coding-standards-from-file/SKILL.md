---
name: write-coding-standards-from-file
description: 'Create or update coding standards documentation by inferring style and conventions from one file, multiple files, or a folder.'
argument-hint: fileName (required), optional folderName and instructions
---

# Write Coding Standards From File

## Inputs

- `fileName` (required)
- `folderName` (optional)
- `instructions` (optional)

## Procedure

1. Inspect the provided file(s).
2. Infer naming, formatting, comments, error handling, and testing style.
3. Identify inconsistencies and majority conventions.
4. Generate a practical coding standards document.
5. Optionally include a lightweight enforcement checklist.

## Output

- Standards document with clear sections
- Optional inconsistency findings
- Optional follow-up actions to align codebase style
