# Sprint 03 Tasks - Firebase Leaderboard Cleanup

Legend: todo | in-progress | done | blocked | deferred

## T-01 - Remove leaderboard override reads

- Backlog ref: B-028
- Owner: `meta`
- Priority: P1
- Status: done
- Dependencies: none
- Files:
  - `Assets/Scripts/UnityAdapter/UI/HighScoreTableView.cs`
  - `Assets/Scripts/UnityAdapter/Social/LeaderboardManager.cs`
- Acceptance:
  - [x] Visible leaderboard rows read only `highScore` or `weeklyHighScore` from Firestore.
  - [x] `scoreOverride` and `weeklyScoreOverride` no longer affect rendered rows.
- Verification:
  - [x] `rg "scoreOverride|weeklyScoreOverride" Assets/Scripts/UnityAdapter`

## T-02 - Filter fabricated guest leaderboard rows

- Backlog ref: B-028
- Owner: `meta`
- Priority: P1
- Status: done
- Dependencies: T-01
- Files:
  - `Assets/Scripts/UnityAdapter/UI/HighScoreTableView.cs`
  - `Assets/Scripts/UnityAdapter/Social/FirebaseManager.cs`
- Acceptance:
  - [x] Current anonymous player may still render as `Sen`.
  - [x] Other guest rows without real usernames do not appear as unrelated fake players.
  - [x] Public leaderboard writes no longer synthesize `Oyuncu XXXX` names for display.
- Verification:
  - [x] `rg "Oyuncu " Assets/Scripts/UnityAdapter`

## T-03 - Verify Firebase-only leaderboard flow

- Backlog ref: B-028
- Owner: `meta`
- Priority: P1
- Status: done
- Dependencies: T-01, T-02
- Files:
  - `sprints/active/sprint-03/report.md`
- Acceptance:
  - [x] Report records build and grep evidence.
  - [x] Remaining manual Firebase/PlayMode validation risk is called out.
- Verification:
  - [x] `dotnet build Assembly-CSharp.csproj -v:minimal`
  - [x] `dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal`
  - [x] `git diff --check`
