# Statistics and Storage Keys

This document lists PlayerPrefs keys written during move flow and game-over flow.

## Move Flow Writes

- `BlokDunyasi_Statistics`
  - Writer: `StatisticsStore.SaveStatistics(...)`
  - Trigger: every successful move (`StatisticsManager.RecordMove(...)` from `GameBootstrap.TryPlaceBlock(...)`)
  - Payload fields affected: `HighestSingleMoveScore`, `MostLinesClearedAtOnce`

- `BlokDunyasi_BestScore`
  - Writer: `BestScoreStore.SetBestScore(...)`
  - Trigger: successful move only when current score beats previous best
  - Payload: integer best score

- `BlockPuzzle_GameData_default`
  - Writer: `UnityPlayerPrefsDataProvider.SaveGameDataAsync(...)` via `GameSaveManager.SaveGameIfNeeded(...)`
  - Trigger: successful move while game is not over
  - Payload: serialized `GameData` snapshot (`Score`, `ComboStreak`, `ScoreFormulaVersion`, board, active blocks, etc.)

## Game-Over Writes

- `BlokDunyasi_Statistics`
  - Writer: `StatisticsStore.SaveStatistics(...)`
  - Trigger: game-over pipeline (`StatisticsManager.RecordGameSession(...)`)
  - Payload fields affected: `HighScore`, `GamesPlayed`, `TotalScore`, `RecentScores`, `TopScores`, `HighestCombo`, etc.

- `BlockPuzzle_GameData_default`
  - Writer: delete path `UnityPlayerPrefsDataProvider.DeleteGameDataAsync(...)`
  - Trigger: game-over pipeline (`GameSaveManager.ClearSavedGame()`)
  - Effect: removes in-progress save snapshot
