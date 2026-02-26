# Exit Criteria (9/10)

## 1) Critical score bug count

- Target: `0`
- Evidence: `Regression` test category must be fully green in CI.

## 2) Score core coverage (formula/combo path)

- Target: high and green.
- Gate:
  - `Rules` namespace line coverage `>= 80%`
- Evidence: CI coverage step with include filter for `BlockPuzzle.Core.Rules.*`.

## 3) Deterministic replay drift

- Target: `0`
- Evidence: `Replay` test category green (`same seed + same moves => same score/state`).

## 4) Save/load score-combo consistency

- Target: `0` inconsistency
- Evidence:
  - round-trip save/load tests in `Integration` category green
  - formula version persisted and restored path validated.

## 5) Live score anomaly ratio

- Target: very low
- Operational thresholds:
  - D7: anomaly ratio `< 0.20%`
  - D14: anomaly ratio `< 0.10%`
- Metric formula:
  - `score_anomaly_ratio = count(move_scored where is_score_anomaly=true) / count(move_scored)`
- Evidence: production telemetry dashboard/query based on `AnalyticsEventData`.
