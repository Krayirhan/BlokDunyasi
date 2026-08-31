# Sprint Template

Copy this template into `sprints/active/sprint-XX/` and split it into `brief.md`, `tasks.md`, and `report.md`.

## brief.md

```markdown
# Sprint XX Brief - [Title]

- Status: draft/approved/in-progress/closed
- Date: YYYY-MM-DD
- Target duration: X days
- Lead owner: `owner`
- Supporting owners: `owner`, `owner`

## Goal

[One short paragraph describing the outcome.]

## Success Criteria

- [ ] Observable result 1
- [ ] Observable result 2
- [ ] Required verification passes

## Scope

- [Included work]

## Out Of Scope

- [Explicitly excluded work]

## Risks

- [Risk and mitigation]

## Required Verification

```powershell
dotnet build Assembly-CSharp.csproj -v:minimal
dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal
dotnet test Tests\BlockPuzzle.Core.Tests\BlockPuzzle.Core.Tests.csproj -v:minimal
git diff --check
```
```

## tasks.md

```markdown
# Sprint XX Tasks - [Title]

Legend: todo | in-progress | done | blocked | deferred

## T-01 - [Task title]

- Backlog ref: B-XXX
- Owner: `owner`
- Priority: P1
- Status: todo
- Dependencies: none
- Files:
  - `Assets/Scripts/...`
- Acceptance:
  - [ ] Concrete observable result
- Verification:
  - [ ] Command/manual check
- Notes:
  - Keep notes short and factual.
```

## report.md

```markdown
# Sprint XX Report - [Title]

- Status: open/closed
- Close date: YYYY-MM-DD

## Summary

[What changed and why.]

## Completed Tasks

| Task | Result | Evidence |
|---|---|---|
| T-01 | done | `command` passed |

## Not Completed

| Task | Status | Reason | Backlog ref |
|---|---|---|---|

## Verification

```text
[Paste concise command results.]
```

## Risks Left

- [Risk]

## Follow-Up Backlog

- B-XXX - [Title]
```
