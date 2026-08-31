# Sprint 03 Report - Firebase Leaderboard Cleanup

- Status: closed
- Close date: 2026-06-07

## Summary

Leaderboard rendering is now Firebase-only on the client side. Score screens no longer read `scoreOverride` or `weeklyScoreOverride`, public leaderboard writes no longer fabricate `Oyuncu XXXX` names, and visible rows now suppress unrelated anonymous guest entries while still mapping the current anonymous user to `Sen` in the UI.

## Completed Tasks

| Task | Result | Evidence |
|---|---|---|
| T-01 | Done | `HighScoreTableView` and `LeaderboardManager` now read only `highScore` / `weeklyHighScore` for visible rows. |
| T-02 | Done | Guest rows are filtered unless they belong to the current user; fabricated `Oyuncu XXXX` output was removed from leaderboard writes. |
| T-03 | Done | `dotnet build Assembly-CSharp.csproj -v:minimal` and `dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal` passed; targeted diff check for touched files passed. |

## Not Completed

None.

## Verification

```text
dotnet build Assembly-CSharp.csproj -v:minimal
dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal
rg "scoreOverride|weeklyScoreOverride|Oyuncu " Assets/Scripts
git diff --check -- Assets/Scripts/UnityAdapter/Social/FirebaseManager.cs Assets/Scripts/UnityAdapter/Social/LeaderboardManager.cs Assets/Scripts/UnityAdapter/UI/HighScoreTableView.cs sprints/active/sprint-03 sprints/backlog.md

Note: full-repo `git diff --check` is currently blocked by pre-existing trailing whitespace in `Assets/Resources/TMP/MalgunGothic_DynamicSDF.asset`.
```

## Risks Left

- Manual Firebase/PlayMode validation is still required.
- Existing guest documents already stored in Firestore are not migrated or deleted by this sprint; they are filtered in the client UI.

## Follow-Up Backlog

- None yet.
