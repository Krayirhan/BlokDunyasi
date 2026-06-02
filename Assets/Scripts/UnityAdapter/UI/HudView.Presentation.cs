using BlockPuzzle.UnityAdapter.Animation;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Configuration;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.UI
{
    public partial class HudView
    {
        /// <summary>
        /// Presentation helper for score, combo, status and target progress updates.
        /// </summary>
        private static class HudPresentationPresenter
        {
            public static void UpdateScoreDisplay(HudView owner, int currentScore, int bestScore, bool isNewBest)
            {
                owner._targetScore = currentScore;

                if (owner._scoreCountAnimation != null)
                    owner.StopCoroutine(owner._scoreCountAnimation);

                owner._scoreCountAnimation = owner.StartCoroutine(owner.AnimateScoreCount());

                if (owner.bestScoreText != null)
                {
                    owner.bestScoreText.text = $"{bestScore:N0}";
                    owner.RestoreBestScoreTextPosition();

                    if (isNewBest && !UISettingsProfile.IsReduceMotionEnabled())
                        owner.StartCoroutine(owner.FlashNewBest());
                }
            }

            public static void UpdateScoreBreakdownDebug(HudView owner, ScoreBreakdownInfo breakdown)
            {
                owner.ApplyScoreBreakdownDebugVisibility();

                if (!owner.showScoreBreakdownDebug || owner.scoreBreakdownText == null)
                    return;

                if (breakdown.FormulaVersion <= 0)
                {
                    owner.scoreBreakdownText.text = "No score breakdown yet";
                    return;
                }

                owner.scoreBreakdownText.text =
                    $"v{breakdown.FormulaVersion} | base:{breakdown.BaseScore} " +
                    $"line x{breakdown.LineClearMultiplier:F2} combo x{breakdown.ComboMultiplier:F2} " +
                    $"=> +{breakdown.ScoreDelta} (total {breakdown.TotalScore})";
            }

            public static void UpdateGameInfo(HudView owner)
            {
                owner.ResolveRequiredDependencies();
                if (owner.gameBootstrap == null)
                    return;

                if (owner.turnCountText != null)
                {
                    var moveCount = owner.gameBootstrap.CurrentState?.MoveCount ?? 0;
                    owner.turnCountText.text = $"{TrEn("Hamle", "Move")}: {moveCount}";
                }

                if (owner.targetGoalSystem != null)
                {
                    int currentScore = owner.gameBootstrap.CurrentScore;
                    var missionProgress = owner.targetGoalSystem.UpdateProgress(currentScore);
                    owner.gameBootstrap.RecordMissionProgress(
                        missionProgress.ProgressDelta,
                        missionProgress.DailyCompleted,
                        missionProgress.WeeklyCompleted);
                }
            }

            public static void ResetTargetGoalSystem(HudView owner)
            {
                if (owner.targetGoalSystem != null)
                    owner.targetGoalSystem.Reset();
            }

            public static void UpdateComboDisplayFromBreakdown(HudView owner, ScoreBreakdownInfo breakdown)
            {
                if (owner.comboText == null)
                    return;

                int comboStreak = breakdown.LinesCleared > 0 ? Mathf.Max(0, breakdown.ComboStreak) : 0;
                if (comboStreak <= 1)
                {
                    HideComboText(owner);
                    return;
                }

                owner.comboText.text = $"Combo {comboStreak}";
                owner.ApplyComboVisualStyle(owner.comboText);
                owner.comboText.gameObject.SetActive(true);

                if (AnimationController.Instance != null && !UISettingsProfile.IsReduceMotionEnabled())
                    AnimationController.Instance.PlayComboBadgeAnim(owner.comboText.gameObject, comboStreak, null);

                if (owner._comboVisibilityRoutine != null)
                    owner.StopCoroutine(owner._comboVisibilityRoutine);

                owner._comboVisibilityRoutine = owner.StartCoroutine(owner.HideComboAfterDelay(owner.comboDisplayDuration));
            }

            public static void HideComboText(HudView owner)
            {
                if (owner.comboText != null)
                    owner.comboText.gameObject.SetActive(false);
            }

            public static void ShowMoveQualityFeedback(HudView owner, ScoreBreakdownInfo breakdown)
            {
                if (owner.gameStatusText == null || breakdown.ScoreDelta <= 0)
                    return;

                string quality = ResolveMoveQualityLabel(breakdown);
                if (string.IsNullOrEmpty(quality))
                    return;

                owner.ShowTransientStatusMessage(quality, 1.1f);
            }
        }
    }
}
