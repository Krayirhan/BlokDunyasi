# Sprint 03 Brief - Firebase Leaderboard Cleanup

- Status: closed
- Date: 2026-06-07
- Target duration: 1-2 days
- Lead owner: `meta`
- Supporting owners: `ui-layout`

## Goal

Make the leaderboard Firebase-only. The scores screen must render only Firestore-backed rows, and leaderboard writes must remain editable from Firebase without local override fields or fabricated public names leaking into the UI.

## Success Criteria

- [ ] Scores screen reads only Firestore leaderboard fields.
- [ ] Dummy/mock/local override leaderboard paths are removed from the visible UI flow.
- [ ] Current anonymous user can still appear as `Sen`, but unrelated anonymous guest rows do not pollute the board.
- [ ] Build and relevant validation commands pass.

## Scope

- Remove leaderboard score override reads.
- Remove fabricated public leaderboard naming from visible rows.
- Keep current-user label substitution in UI only.

## Out Of Scope

- Leaderboard visual redesign.
- Firestore data migration or bulk cleanup of already-written documents.
- Auth flow redesign.

## Risks

- Existing Firestore guest rows may still exist and need to be filtered consistently.
- Anonymous leaderboard support depends on keeping a readable current-user row without inventing visible fake players.

## Required Verification

```powershell
dotnet build Assembly-CSharp.csproj -v:minimal
dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal
rg "scoreOverride|weeklyScoreOverride|Oyuncu " Assets/Scripts
git diff --check
```

## Approval

Approved by user on 2026-06-07.
