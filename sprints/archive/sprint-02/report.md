# Sprint 02 Report - Runtime Stability Baseline

- Status: closed
- Close date: 2026-06-07

## Summary

Production audio is no longer disabled by serialized runtime override values in shipped play mode, and the main menu scene no longer ships with audio playback turned off. Continue reward handling was aligned with `GameBootstrap.TryContinueAfterRewardedAd()` so ad success only counts as success when the run is actually restored.

## Completed Tasks

| Task | Result | Evidence |
|---|---|---|
| T-01 | Done | `dotnet build Assembly-CSharp.csproj -v:minimal` passed; scene grep shows `disableMusicPlayback` and `disableSfxPlayback` are `0` in `MainMenu` and `OyunEkranı`. |
| T-02 | Done | `ContinueEconomyManager` now resolves continue success through `GameBootstrap.TryContinueAfterRewardedAd()`. |
| T-03 | Done | `dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal` passed; rewarded telemetry/fallback paths and `AdMobRuntimeConfig.asset` rewarded IDs remain present. |
| T-04 | Done | Report records command evidence and leaves PlayMode validation as explicit residual risk. |

## Not Completed

None.

## Verification

```text
dotnet build Assembly-CSharp.csproj -v:minimal
dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal
dotnet test Tests\BlockPuzzle.Core.Tests\BlockPuzzle.Core.Tests.csproj -v:minimal
rg "disableMusicPlayback|AudioListener.pause|mute" Assets/Scripts Assets/Scenes
rg "TryContinueAfterRewardedAd|continue_success|Rewarded" Assets/Scripts/UnityAdapter
rg "continue_load_timeout|continue_denied|continue_restore_failed|Rewarded" Assets/Scripts Assets/Resources
git diff --check
```

## Risks Left

- Audio behavior still needs Unity PlayMode or device validation, especially scene transition timing and mixer state.
- Rewarded continue still depends on runtime ad availability and real callback ordering from the ad SDK.

## Follow-Up Backlog

- None yet.
