# Release Checklist

## Pre-Release Quality Gate

- [ ] GitHub Actions workflow `Quality Gate - Core Tests` green on PR.
- [ ] Branch protection requires all `Quality Gate - Core Tests` checks before merge.
- [ ] No skipped regression/replay/performance failures.

## Save Migration

- [ ] `GameData.SaveVersion` changes documented.
- [ ] `ScoreFormulaMigration.MigrateInPlace(...)` updated for new formula/save changes.
- [ ] Migration unit tests updated and passing.
- [ ] Old save sample can be loaded without crash.

## Backward Compatibility

- [ ] Existing players keep score/progression after update.
- [ ] `score_formula_version` written on save and read on load.
- [ ] Non-breaking defaults exist for missing/legacy fields.

## No Data Loss

- [ ] Best score is preserved across update path.
- [ ] In-progress game load/save round-trip keeps board, active blocks, colors, and combo.
- [ ] Statistics keys remain stable (`BlokDunyasi_Statistics`, `BlokDunyasi_BestScore`, `BlockPuzzle_GameData_default`).
- [ ] Game-over cleanup only deletes in-progress snapshot key.

## Analytics Compatibility

- [ ] `schema_version` unchanged for non-breaking telemetry changes.
- [ ] `schema_version` incremented for breaking telemetry schema changes.
- [ ] Telemetry event names unchanged unless explicitly versioned and documented.
