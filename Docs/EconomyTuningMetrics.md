# Economy Tuning Metrics (7/14 Day)

This document defines short-horizon balancing targets for score economy.

## Primary KPIs

- `average_score`: average final score per completed session.
- `session_length_minutes`: average elapsed play time per session.
- `fail_rate`: percentage of sessions ending within target move/time failure bands.
- `score_anomaly_ratio`: ratio of `move_scored` events where `is_score_anomaly=true`.

## Windows

- `D7`: trailing 7-day window (early sensitivity, quick iteration).
- `D14`: trailing 14-day window (stability check, release gate).

## Segmenting

- Segment by difficulty bucket, platform, and new-vs-returning players.
- Read global and per-segment values before tuning.

## D7 Targets (Iteration)

- `average_score`: ±10% from design baseline.
- `session_length_minutes`: 4-8 min median.
- `fail_rate`: 35-55%.
- `score_anomaly_ratio`: `< 0.20%`.

Action rule:
- If two or more KPIs are outside D7 target for 3 consecutive days, apply small tuning step (5-10%) and monitor.

## D14 Targets (Release)

- `average_score`: ±5% from design baseline.
- `session_length_minutes`: 5-9 min median.
- `fail_rate`: 40-50%.
- `score_anomaly_ratio`: `< 0.10%`.

Release rule:
- Do not ship economy changes if any KPI is outside D14 target without explicit product sign-off.

## Data Sources

- Gameplay telemetry (`move_scored`, `line_cleared`, `combo_changed`, `best_score_updated`).
- Session-level statistics store values and game-over summary writes.

## Suggested Derived Metrics

- `score_per_minute = total_score / session_length_minutes`
- `moves_to_fail = move_count at game over`
- `line_clear_rate = total_lines_cleared / move_count`

These are secondary diagnostics; primary tuning decisions stay anchored to average score, session length, and fail rate.
