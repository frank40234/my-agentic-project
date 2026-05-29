---
description: "Global behavior rules for all agents. Applies to every conversation and task."
alwaysApply: true
---

# Global Agent Rules

## Language
- All agent responses, code comments, commit messages, and documentation output must be in **Traditional Chinese (繁體中文)**
- Variable names, function names, class names, and file names must use **English**
- Architecture document keys must use **English**; values can be in Traditional Chinese

## Code Style
- Every function/method must have a docstring or XML doc comment
- Follow .editorconfig if present in the project
- Do NOT introduce any third-party package not listed in the architecture document
- Use meaningful variable and function names (no single-letter variables except loop counters)

## Security
- NEVER hardcode secrets, API keys, passwords, or connection strings in source code
- All user inputs must be validated and sanitized
- Use parameterized queries for all database operations

## Git Conventions
- Branch naming: `feature/task-{id}`
- Commit message format: `feat(TASK-{id}): 簡短中文描述`
- Each task may only modify files within its defined scope
- Always commit on the task branch, never directly on main

## Error Handling
- All API endpoints must return consistent error response format
- All exceptions must be logged with sufficient context
- Never expose internal error details to end users

## File Organization
- Architecture documents go in `architecture/`
- Task artifacts go in `artifacts/`
- Source code goes in `src/` (or framework-specific convention)
- Tests go in `tests/` (or framework-specific convention)
