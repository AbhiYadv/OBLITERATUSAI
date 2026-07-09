# Nexo World Development Rules

## Project Boundary

This directory is the Nexo World game application.

Primary permitted scope:

`apps/nexo-world/**`

Do not modify files outside this directory unless the user explicitly approves the exact files first.

## Current Operating Mode

- Work locally only.
- Do not perform GitHub operations.
- Do not commit, stage, push, pull, fetch, merge, rebase, reset, clean, stash, or switch branches.
- Do not modify worktree configuration.
- Leave integration into `dev` for a later user-approved step.

## Existing Work Protection

- Assume every existing file and modification is intentional.
- Never delete or replace existing work without inspecting it first.
- Preserve incomplete implementation unless the current task explicitly changes it.
- Do not regenerate or modify repository-level `dist/**`.
- Do not touch changes under `apps/desktop/**`.
- Do not copy files between this worktree and the main checkout.

## Implementation Discipline

Before coding:

1. Read the relevant Nexo World files.
2. Identify the smallest implementation boundary.
3. Explain the intended change and files involved.
4. Check for existing local modifications.
5. Stop if the task conflicts with existing work.

During coding:

- Keep changes limited to the requested feature.
- Prefer small, reviewable modules.
- Preserve existing architecture and naming patterns.
- Do not introduce new dependencies without approval.
- Do not expose secrets, API keys, tokens, or credentials.
- Do not store secrets in source files, configuration committed to the repository, logs, or tests.
- Validate all external and user-controlled input.
- Do not weaken authentication, authorization, or security controls.
- Avoid broad refactors during feature work.

## Validation

After changes:

- Run the narrowest relevant tests first.
- Run type checking and linting only when available and relevant.
- Do not run commands that regenerate unrelated repository output.
- Do not silently modify snapshots or generated files.
- Report every validation command and result.
- Report failures honestly and do not hide or bypass them.

## Completion Format

At the end of each task, report:

- Files inspected
- Files changed
- What changed
- Tests or checks run
- Validation results
- Remaining risks or blockers
- Confirmation that no Git or GitHub write operation was performed

Do not commit or integrate the work.
