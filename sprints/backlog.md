# Backlog

All pending sprint-sized work lives here. Sprint planning pulls from this file.

Priority: P0 blocker, P1 important, P2 improvement, P3 nice-to-have.

## P0 - Blockers

### ~~B-001 - Audio playback disabled in production~~ ✓ DONE
- `disableMusicPlayback` / `disableSfxPlayback` → `false` in both code and `MainMenu.unity` serialized data.

### ~~B-002 - Rewarded ad continue mechanism~~ ✓ DONE
- Created `Assets/Scripts/UI/Ads/RewardedAdBridge.cs` — static bridge wiring `AdMobManager` → `ContinueEconomyManager`.

## P1 - Important Work

### B-003 - Split GameBootstrap god object *(IN PROGRESS — extraction wave 1 completed)*
- Owner: `ui-layout`, `persistence`, `core-engine`
- Files:
  - `Assets/Scripts/UnityAdapter/Boot/GameBootstrap.cs`
- Progress: `TutorialService`, `AnalyticsTelemetryService` and `VisualBackgroundManager` are extracted. Remaining bootstrap responsibilities are still in scope.
- Acceptance: All remaining responsibilities are extracted without changing gameplay behavior.

### B-004 - Split NewDragSystem
- Owner: `input`
- Files:
  - `Assets/Scripts/UnityAdapter/Input/NewDragSystem.cs`
- Acceptance: Drag/drop behavior remains unchanged; input, visual and placement responsibilities are separated.

### B-005 - Move NewBlockTray hardcoded positions to profiles
- Owner: `input`, `ui-layout`
- Files:
  - `Assets/Scripts/UnityAdapter/Blocks/NewBlockTray.cs`
- Acceptance: Tray position and scale values come from layout/profile data.

### B-006 - Move ScreenLayoutManager hardcoded layout values to config
- Owner: `ui-layout`
- Files:
  - `Assets/Scripts/UnityAdapter/Boot/ScreenLayoutManager.cs`
- Acceptance: Header, tray, middle gap and footer spacing come from profile/config.

### B-007 - Move GameBootstrap camera parameters to profile/config
- Owner: `ui-layout`
- Files:
  - `Assets/Scripts/UnityAdapter/Boot/GameBootstrap.cs`
- Acceptance: Camera position and adaptive size values are configurable.

### B-008 - Deploy safe area to main scenes
- Owner: `ui-layout`
- Files:
  - `Assets/Scenes/MainMenu.unity`
  - `Assets/Scenes/OyunEkranı.unity`
  - `Assets/Scenes/Scores.unity`
- Acceptance: All main scenes have safe-area root/fitter and standard canvas scaling.

### B-009 - Split MainMenuController
- Owner: `ui-layout`
- Files:
  - `Assets/Scripts/UnityAdapter/UI/MainMenuController.cs`
- Acceptance: Responsibilities are separated while preserving backward compatibility.

### B-010 - Save data anti-cheat hook
- Owner: `persistence`, `meta`
- Files:
  - `Assets/Scripts/UnityAdapter/Social/ScoreValidator.cs`
  - `Assets/Scripts/Core/Persistence/GameData.cs`
- Acceptance: Loaded score data is validated through score validation rules.

### B-028 - Leaderboard must be Firebase-only
- Owner: `meta`
- Files:
  - `Assets/Scripts/UnityAdapter/UI/HighScoreTableView.cs`
  - `Assets/Scripts/UnityAdapter/Social/LeaderboardManager.cs`
  - `Assets/Scripts/UnityAdapter/Social/FirebaseManager.cs`
- Acceptance: Leaderboard reads and writes use only Firestore-backed fields; no local override/mock data or synthesized public rows are shown.

### B-029 - Complete iOS AdMob release configuration
- Owner: `build-release`, `monetization`
- Files:
  - `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`
  - `Assets/Resources/AdMobRuntimeConfig.asset`
- Acceptance: iOS App ID and banner/interstitial/rewarded production unit IDs are supplied from the AdMob console and verified in an iOS development build.

## P2 - Improvements

### B-024 - Extract GameOver scene navigation
- Owner: `ui-layout`
- Files:
  - `Assets/Scripts/UnityAdapter/UI/GameOverView.cs`
- Acceptance: Restart, main-menu and optional dedicated GameOver scene routing live behind a focused navigation service without changing user-visible flow.

### ~~B-025 - Extract GameOver continue-offer state machine~~ ✓ DONE
- Owner: `monetization`, `ui-layout`
- Files:
  - `Assets/Scripts/UnityAdapter/UI/GameOverView.cs`
- Completed by `Assets/Scripts/UnityAdapter/UI/ContinueOfferController.cs`; Unity script compilation passed on 2026-07-22. Manual rewarded-ad device smoke test remains recommended.

### B-026 - Extract GameOver localization/messages
- Owner: `ui-layout`
- Files:
  - `Assets/Scripts/UnityAdapter/UI/GameOverView.cs`
- Acceptance: GameOver labels, guidance text and continue-offer messages come from a dedicated message provider.

### B-027 - Extract GameOver final VFX
- Owner: `ui-layout`
- Files:
  - `Assets/Scripts/UnityAdapter/UI/GameOverView.cs`
- Acceptance: Board explosion timing, burst selection and particle spawning move behind a focused VFX component with the same visual behavior.

### B-011 - Convert BlockSpawner magic numbers to constants/config
- Owner: `core-engine`
- Files:
  - `Assets/Scripts/Core/RNG/BlockSpawner.cs`
- Acceptance: Thresholds are named constants or config values.

### B-012 - Move DifficultyModel CircularBuffer to separate file
- Owner: `core-engine`
- Files:
  - `Assets/Scripts/Core/RNG/DifficultyModel.cs`
- Acceptance: `CircularBuffer<T>` is its own file and DifficultyModel uses it.

### B-013 - Refactor GameEngine ExecuteMove
- Owner: `core-engine`
- Files:
  - `Assets/Scripts/Core/Engine/GameEngine.cs`
- Acceptance: `ExecuteMove()` is shorter and split into focused private helpers.

### B-014 - Move GameData migration logic to separate class
- Owner: `persistence`
- Files:
  - `Assets/Scripts/Core/Persistence/GameData.cs`
- Acceptance: Migration chain is testable through a dedicated migrator.

### B-015 - Remove redundant LineDetector double-checks
- Owner: `core-engine`
- Files:
  - `Assets/Scripts/Core/Board/LineDetector.cs`
- Acceptance: Validation stays O(n) and duplicate checks are removed.

### B-016 - Rename duplicate Systems/GameBootstrap
- Owner: `build-release`
- Files:
  - `Assets/Scripts/Systems/GameBootstrap.cs`
- Acceptance: Startup class no longer conflicts conceptually with `UnityAdapter.Boot.GameBootstrap`.

### B-017 - Ignore build artifacts
- Owner: `build-release`
- Files:
  - `.gitignore`
- Acceptance: Build outputs, logs and local keystore files are ignored.

### B-018 - Support tall phones 9:20+
- Owner: `ui-layout`
- Files:
  - `DeviceLayoutProfile` assets
  - `Assets/Scripts/UnityAdapter/Boot/ScreenLayoutManager.cs`
- Acceptance: Tall phone profile avoids board/tray overlap.

### B-019 - Improve ghost preview visuals
- Owner: `input`, `ui-layout`
- Files:
  - `Assets/Scripts/UnityAdapter/Grid/SimpleGridView.cs`
- Acceptance: Ghost preview uses theme-compatible translucent visuals.
