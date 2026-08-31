# Sprint 02 Brief - Runtime Stability Baseline

- Status: closed
- Date: 2026-06-07
- Target duration: 2-3 days
- Lead owner: `audio`
- Supporting owners: `monetization`, `build-release`, `ui-layout`

## Goal

Restore baseline runtime stability in two blocked production flows: audio playback and rewarded continue. This sprint should leave the app with reliable music behavior in production and a safe rewarded continue path that either restores the run or fails cleanly.

## Success Criteria

- [ ] Production build music works and no debug-only audio disable path remains active.
- [ ] Rewarded continue succeeds when reward is earned.
- [ ] Rewarded continue failure and timeout cases fall back safely.
- [ ] Build and relevant validation commands pass.

## Scope

- Fix `AudioManager` production playback gating.
- Stabilize rewarded continue flow and fallback behavior.
- Validate relevant ad/runtime configuration references if they block B-002 acceptance.

## Out Of Scope

- Broad audio system redesign.
- Ad SDK provider migration.
- UI redesign of continue or audio settings surfaces.
- Refactors unrelated to B-001 or B-002.

## Risks

- Audio issues may be partially configuration-driven rather than code-only.
- Rewarded continue spans runtime ads state, UI timing, and restore logic.
- Unity Editor and generated Android files may already be dirty in the worktree.

## Required Verification

```powershell
dotnet build Assembly-CSharp.csproj -v:minimal
dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal
rg "disableMusicPlayback|mute|AudioListener|reward" Assets/Scripts Assets/Scenes Assets/Resources
git diff --check
```

## Approval

Approved by user on 2026-06-07.
