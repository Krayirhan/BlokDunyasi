# Sprint Roadmap

- Planning date: 2026-06-07
- Source: `sprints/backlog.md`
- Rule: only one sprint may live in `sprints/active/` at a time; future work stays in this roadmap until promoted.

## Sequence

| Sprint | Title | Primary backlog | Lead owner | Target duration |
|---|---|---|---|---|
| 02 | Runtime Stability Baseline | B-001, B-002 | `audio` | 2-3 days |
| 03 | Responsive Layout Baseline | B-005, B-006, B-007, B-008, B-018 | `ui-layout` | 3-4 days |
| 04 | GameOver Extraction Wave | B-024, B-026, B-027 | `ui-layout` | 3-4 days |
| 05 | Input Pipeline Cleanup | B-004, B-019 | `input` | 3-4 days |
| 06 | Bootstrap and Menu Decomposition | B-003, B-009 | `ui-layout` | 4-5 days |
| 07 | Data Integrity and Persistence | B-010, B-014 | `persistence` | 2-3 days |
| 08 | Core Engine Cleanup | B-011, B-012, B-013, B-015 | `core-engine` | 3-4 days |
| 09 | Build and Release Hygiene | B-016, B-017 | `build-release` | 1-2 days |

## Sprint 02 - Runtime Stability Baseline

- Backlog: B-001, B-002
- Lead owner: `audio`
- Supporting owners: `monetization`, `build-release`, `ui-layout`
- Goal: restore production audio and make rewarded continue deterministic before wider refactors.
- Why now: both items are P0 blockers and affect baseline runtime quality.
- Exit gate: production audio no longer depends on debug-only disable paths; rewarded continue succeeds or fails safely with verified fallback.

## Sprint 03 - Responsive Layout Baseline

- Backlog: B-005, B-006, B-007, B-008, B-018
- Lead owner: `ui-layout`
- Supporting owners: `input`
- Goal: move hardcoded layout and camera values into profiles/config and finish safe-area deployment.
- Why now: layout hardcoding blocks device coverage and multiplies risk for later UI refactors.
- Exit gate: tray, screen spacing, camera sizing and tall-phone behavior come from profile/config with scene-safe-area coverage intact.

## Sprint 04 - GameOver Extraction Wave

- Backlog: B-024, B-026, B-027 (`B-025` completed in extraction wave)
- Lead owner: `ui-layout`
- Supporting owners: `monetization`
- Goal: reduce `GameOverView` risk by extracting navigation, continue state, messages and VFX into focused collaborators.
- Why now: Sprint 01 stabilized behavior; next step is structural risk reduction without changing UX.
- Exit gate: navigation, localization and VFX are delegated out while preserving telemetry and current screen flow. Continue-offer state extraction is already complete.

## Sprint 05 - Input Pipeline Cleanup

- Backlog: B-004, B-019
- Lead owner: `input`
- Supporting owners: `ui-layout`
- Goal: separate drag responsibilities and improve ghost preview visuals without changing placement behavior.
- Why now: input logic is a high-change surface and should be made testable before more UI iteration.
- Exit gate: drag routing, visualization and placement responsibilities are split; ghost preview matches the visual system.

## Sprint 06 - Bootstrap and Menu Decomposition

- Backlog: B-003, B-009
- Lead owner: `ui-layout`
- Supporting owners: `persistence`, `core-engine`
- Goal: break down the two largest Unity adapter godfiles that coordinate runtime setup and menu flow.
- Why now: these files sit on critical paths and obstruct safe feature work across multiple owners.
- Exit gate: `GameBootstrap` and `MainMenuController` have smaller focused collaborators with no gameplay/menu regressions.

## Sprint 07 - Data Integrity and Persistence

- Backlog: B-010, B-014
- Lead owner: `persistence`
- Supporting owners: `meta`
- Goal: enforce score validation on load and isolate migration logic into a testable component.
- Why now: persistence integrity should be locked before further leaderboard or progression work.
- Exit gate: save-load path applies score validation and migration logic is covered through dedicated tests.

## Sprint 08 - Core Engine Cleanup

- Backlog: B-011, B-012, B-013, B-015
- Lead owner: `core-engine`
- Supporting owners: `none`
- Goal: reduce engine complexity in focused internal areas without changing gameplay output.
- Why now: these are lower-risk core cleanups after runtime-facing blockers are under control.
- Exit gate: constants/config replace magic thresholds, helper structures move out cleanly, and move execution/line detection stay behaviorally stable.

## Sprint 09 - Build and Release Hygiene

- Backlog: B-016, B-017
- Lead owner: `build-release`
- Supporting owners: `none`
- Goal: remove naming ambiguity in startup code and keep build artifacts out of version control noise.
- Why now: this work is useful but not blocking product behavior, so it comes after runtime and architecture stabilization.
- Exit gate: duplicate bootstrap naming is resolved and repository ignore rules cover local release outputs.
