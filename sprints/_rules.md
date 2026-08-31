# Sprint Rules

These rules define how sprint work is planned, executed, and closed in this repository.
Change this file only when the user explicitly asks to change the sprint process.

## 1. Directory Contract

Each sprint has exactly this shape:

```text
sprints/active/sprint-XX/
  brief.md
  tasks.md
  report.md
```

Archive path:

```text
sprints/archive/sprint-XX/
```

Naming:

- Sprint folder names use two digits: `sprint-01`, `sprint-02`, `sprint-03`.
- There can be only one folder inside `sprints/active/`.
- `sprints/backlog.md` is the only backlog source.

## 2. Sprint Lifecycle

```text
Backlog -> Brief -> Approval -> Tasks -> Work -> Report -> Archive
```

Required gates:

- Brief gate: `brief.md` exists before implementation starts.
- Approval gate: sprint starts only after explicit user approval or a direct user instruction to execute that sprint.
- Task gate: every implementation maps to a task ID in `tasks.md`.
- Verification gate: every done task includes evidence.
- Closure gate: `report.md` is completed before archiving.

## 3. Scope Control

- Do not silently expand sprint scope.
- Newly discovered bugs go to `sprints/backlog.md`.
- A new issue can enter the active sprint only if it blocks a current P0/P1 task or the user explicitly approves it.
- Refactor is not a goal by itself; it must protect or enable a concrete behavior.

## 4. Priority Rules

| Priority | Meaning |
|---|---|
| P0 | App cannot build, run, or a critical production flow is broken |
| P1 | Important user-facing bug, release blocker, data integrity issue |
| P2 | Refactor, cleanup, maintainability, non-blocking improvement |
| P3 | Nice-to-have |

Execution order:

- P0 before P1.
- P1 before P2 unless user says otherwise.
- P2 cannot destabilize release-critical flows.

## 5. Task Format

Every task in `tasks.md` must include:

```markdown
### T-XX - Title
- Backlog ref: B-XXX or `none`
- Owner: `agent-name`
- Priority: P0/P1/P2/P3
- Status: todo/in-progress/done/blocked
- Dependencies: T-XX or `none`
- Files:
  - `path/to/file`
- Acceptance:
  - [ ] Concrete observable result
- Verification:
  - [ ] Command or manual validation
```

Allowed statuses:

- `todo`
- `in-progress`
- `done`
- `blocked`
- `deferred`

Only one task should be `in-progress` at a time unless tasks are independent and explicitly parallel.

## 6. Ownership Boundaries

Use these owners:

| Owner | Scope |
|---|---|
| `core-engine` | Pure C# gameplay rules |
| `persistence` | Save/load, migrations, data models |
| `ui-layout` | Unity UI, scenes, responsive layout |
| `input` | Drag, touch, placement |
| `audio` | Music, SFX, audio routing |
| `meta` | Leaderboard, Firebase social, missions, achievements |
| `monetization` | Ads, rewarded flow, IAP, economy |
| `build-release` | Build settings, Gradle, store, release |

Cross-owner changes require a lead owner in `brief.md`.

## 7. Verification Matrix

Default commands:

```powershell
dotnet build Assembly-CSharp.csproj -v:minimal
dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal
dotnet test Tests\BlockPuzzle.Core.Tests\BlockPuzzle.Core.Tests.csproj -v:minimal
git diff --check
```

Minimum expectations:

- Core changes: run core tests.
- Unity adapter/UI changes: run `Assembly-CSharp` build.
- Ads/Firebase/build changes: run adapter build and relevant config search.
- Scene edits: run missing script search with `rg "m_Script: \{fileID: 0\}" Assets\Scenes Assets\Prefabs`.

## 8. Done Criteria

A task is `done` when:

- Acceptance criteria pass.
- Required verification is recorded.
- No known P0/P1 regression is hidden.
- Any follow-up work is added to backlog.

A sprint is `done` when:

- All P0/P1 tasks are `done` or explicitly deferred by the user.
- `report.md` contains verification evidence.
- Unfinished work is moved to backlog.
- The active sprint can be moved to `sprints/archive/`.

## 9. Git Policy

- Do not commit unless the user explicitly asks.
- Do not rewrite history.
- Do not revert unrelated dirty files.
- Mention dirty worktree risks in sprint report when relevant.
