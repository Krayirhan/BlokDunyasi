# Sprint 01 Tasks - GameOver Flow Stabilization

Legend: todo | in-progress | done | blocked | deferred

## T-01 - Final game-over must show in-scene panel

- Backlog ref: B-020
- Owner: `ui-layout`
- Priority: P1
- Status: done
- Dependencies: none
- Files:
  - `Assets/Scripts/UnityAdapter/UI/GameOverView.cs`
  - `Assets/Scenes/OyunEkranı.unity`
- Acceptance:
  - [x] When `useSeparateGameOverScene` is false, finalization calls the in-scene result panel instead of loading `MainMenu`.
  - [x] Main menu loading remains available only through the main menu button.
  - [x] Restart still reloads the gameplay scene.
- Verification:
  - [x] `dotnet build Assembly-CSharp.csproj -v:minimal`
  - [ ] Manual Unity PlayMode check recommended.

## T-02 - Continue exhausted path must not trap player

- Backlog ref: B-021
- Owner: `ui-layout`
- Priority: P1
- Status: done
- Dependencies: T-01
- Files:
  - `Assets/Scripts/UnityAdapter/UI/GameOverView.cs`
- Acceptance:
  - [x] If continue quota is exhausted, the player can reach final score/restart/main-menu state.
  - [x] No unavailable-offer dead end remains.
  - [x] Continue telemetry events remain intact.
- Verification:
  - [x] `dotnet build Assembly-CSharp.csproj -v:minimal`

## T-03 - Preserve rewarded continue success path

- Backlog ref: B-022
- Owner: `monetization`
- Priority: P1
- Status: done
- Dependencies: T-01
- Files:
  - `Assets/Scripts/UnityAdapter/UI/GameOverView.cs`
  - `Assets/Scripts/UnityAdapter/Monetization/ContinueEconomyManager.cs`
- Acceptance:
  - [x] Reward earned still calls continue restore.
  - [x] Reward close without reward falls back to final game-over.
  - [x] Reward load timeout falls back to final game-over.
- Verification:
  - [x] `dotnet build Assembly-CSharp.csproj -v:minimal`
  - [x] `dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal`

## T-04 - Add follow-up extraction plan

- Backlog ref: B-023
- Owner: `ui-layout`
- Priority: P2
- Status: done
- Dependencies: T-01, T-02
- Files:
  - `sprints/backlog.md`
- Acceptance:
  - [x] Follow-up tasks exist for extracting scene navigation, continue-offer state, localization, and VFX from `GameOverView`.
  - [x] No broad refactor is performed inside this sprint.
- Verification:
  - [x] Backlog entries are present and scoped.
