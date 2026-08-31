# Sprint 02 Tasks - Runtime Stability Baseline

Legend: todo | in-progress | done | blocked | deferred

## T-01 - Remove production audio disable path

- Backlog ref: B-001
- Owner: `audio`
- Priority: P0
- Status: done
- Dependencies: none
- Files:
  - `Assets/Scripts/UnityAdapter/Audio/AudioManager.cs`
  - `Assets/Scenes/MainMenu.unity`
  - `Assets/Scenes/OyunEkranı.unity`
- Acceptance:
  - [x] Production playback does not depend on a debug-only disable flag.
  - [x] Main gameplay and menu scenes still route music through the current audio manager.
  - [x] No new null-reference or missing-source regression is introduced.
- Verification:
  - [x] `dotnet build Assembly-CSharp.csproj -v:minimal`
  - [x] `rg "disableMusicPlayback|AudioListener.pause|mute" Assets/Scripts Assets/Scenes`

## T-02 - Stabilize rewarded continue success path

- Backlog ref: B-002
- Owner: `monetization`
- Priority: P0
- Status: done
- Dependencies: none
- Files:
  - `Assets/Scripts/UnityAdapter/Monetization/ContinueEconomyManager.cs`
  - `Assets/Scripts/UnityAdapter/UI/GameOverView.cs`
- Acceptance:
  - [x] Reward earned path restores the run through the active continue mechanism.
  - [x] Continue consumption is recorded only on successful restore.
  - [x] Success path does not strand the player on the game-over UI.
- Verification:
  - [x] `dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal`
  - [x] `rg "TryContinueAfterRewardedAd|continue_success|Rewarded" Assets/Scripts/UnityAdapter`

## T-03 - Stabilize rewarded continue failure fallback

- Backlog ref: B-002
- Owner: `monetization`
- Priority: P0
- Status: done
- Dependencies: T-02
- Files:
  - `Assets/Scripts/UnityAdapter/Monetization/ContinueEconomyManager.cs`
  - `Assets/Scripts/UnityAdapter/UI/GameOverView.cs`
  - `Assets/Resources/AdMobRuntimeConfig.asset`
- Acceptance:
  - [x] Load failure, no-reward close, and timeout cases converge on a safe final state.
  - [x] Failure path keeps telemetry and does not double-consume continue state.
  - [x] Required runtime config references for rewarded ads remain present.
- Verification:
  - [x] `dotnet build Assembly-CSharp.csproj -v:minimal`
  - [x] `dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal`
  - [x] `rg "continue_load_timeout|continue_denied|continue_restore_failed|Rewarded" Assets/Scripts Assets/Resources`

## T-04 - Record runtime blocker verification

- Backlog ref: `none`
- Owner: `build-release`
- Priority: P1
- Status: done
- Dependencies: T-01, T-03
- Files:
  - `sprints/active/sprint-02/report.md`
- Acceptance:
  - [x] Sprint report records build and grep evidence for audio and rewarded continue blockers.
  - [x] Remaining PlayMode-only risks are called out explicitly.
- Verification:
  - [x] `git diff --check`
