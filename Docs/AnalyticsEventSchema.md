# Analytics Event Schema

This document defines the gameplay telemetry schema emitted by `GameBootstrap`.

## Schema Versioning

- `schema_version`: `1`
- C# constant source: `GameBootstrap.AnalyticsSchemaVersion`
- Rule: when payload fields are added/removed/renamed in a breaking way, increment `schema_version`.
- Rule: keep old fields until analytics pipeline confirms migration to avoid dashboard breakage.

## Event Names

- `move_scored`
- `line_cleared`
- `combo_changed`
- `best_score_updated`

## Shared Payload Fields

Every event carries the same payload shape (`AnalyticsEventData`):

- `event_name` (`string`)
- `schema_version` (`int`)
- `score_formula_version` (`int`)
- `session_move_count` (`int`)
- `total_score` (`int`)
- `score_delta` (`int`)
- `lines_cleared` (`int`)
- `combo_before` (`int`)
- `combo_after` (`int`)
- `best_score` (`int`)
- `is_new_best` (`bool`)
- `is_score_anomaly` (`bool`)
- `score_anomaly_code` (`string`)
- `timestamp_unix_ms` (`long`)

## Emission Rules

- `move_scored`: emitted on every successful move.
- `line_cleared`: emitted when `lines_cleared > 0`.
- `combo_changed`: emitted when `combo_before != combo_after`.
- `best_score_updated`: emitted when best score transaction marks new best.

## Score Anomaly Semantics

`is_score_anomaly=true` means at least one score integrity invariant failed on move processing.

Current anomaly codes:

- `NEGATIVE_SCORE_DELTA`
- `NEGATIVE_TOTAL_SCORE`
- `SCORE_WITHOUT_LINES`
- `LINES_WITHOUT_SCORE`
- `COMBO_NOT_INCREASED_AFTER_CLEAR`
- `COMBO_INCREASED_WITHOUT_CLEAR`
