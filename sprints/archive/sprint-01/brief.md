# Sprint 01 Brief - GameOver Flow Stabilization

- Status: closed
- Date: 2026-06-07
- Target duration: 1-2 days
- Lead owner: `ui-layout`
- Supporting owners: `monetization`, `build-release`

## Goal

Stabilize the game-over flow before larger refactors. The player must reliably see the in-scene game-over result panel after no moves remain, rewarded continue expires, or continue rights are exhausted.

## Success Criteria

- [ ] Final game-over no longer auto-loads `MainMenu` when `useSeparateGameOverScene` is disabled.
- [ ] Continue-rights-exhausted path reaches a final result state and does not trap the player on an unavailable offer panel.
- [ ] Existing restart and main menu buttons still work.
- [ ] Build and core tests pass.

## Scope

- Fix `GameOverView` finalization behavior.
- Keep the current in-scene `GameOverPanel` model.
- Add guardrails around continue-offer state transitions.
- Document follow-up godfile extraction tasks in backlog if needed.

## Out Of Scope

- Full `GameOverView` rewrite.
- New dedicated `GameOver.unity` scene.
- Visual redesign of the game-over screen.
- Ads SDK behavior changes beyond the continue state machine.

## Risks

- `GameOverView` is a godfile and has tightly coupled UI, ads, scene navigation, VFX, and localization.
- Scene serialized references may be stale when runtime layout builders replace child UI.
- Unity PlayMode validation may still be needed after CLI builds pass.

## Required Verification

```powershell
dotnet build Assembly-CSharp.csproj -v:minimal
dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal
dotnet test Tests\BlockPuzzle.Core.Tests\BlockPuzzle.Core.Tests.csproj -v:minimal
git diff --check
```

## Approval

Approved by user on 2026-06-07.
