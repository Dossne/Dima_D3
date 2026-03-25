# AGENTS.md

## Project
Unity mobile prototype (URP) for iterative AI-driven development.

## Core workflow
task -> execute -> proof -> fix -> repeat

## Rules
- Work on one task at a time.
- Do not expand scope beyond the current task.
- Ask for approval before destructive changes.
- Ask for approval before changing packages, ProjectSettings, or deleting files.
- Prefer minimal changes over broad refactors.
- Player-facing changes require manual review.
- Stop after errors; do not continue by stacking more changes.
- Always preserve rollbackability.

## Unity repo boundaries
Allowed by default:
- Assets/
- Packages/
- ProjectSettings/ only if explicitly requested

Do not modify without explicit approval:
- ProjectSettings/
- Packages/manifest.json
- packages-lock.json
- build settings
- third-party SDK setup

## Validation expectations
After each implementation task:
- check compile status
- inspect logs
- verify affected files only
- summarize changed files
- state proof of completion
- list risks / follow-up issues

## Output style
- Be practical and concise
- State exactly what files will change before changing them
- For risky edits, propose plan first