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

                var sb = breakdown.Breakdown;
                owner.scoreBreakdownText.text = 
                    (sb.PlacementScore > 0 ? $"<color=grey>+{sb.PlacementScore} placement</color>\n" : "") +
                    (sb.LineClearScore > 0 ? $"<color=orange>+{sb.LineClearScore:N0} {sb.LinesCleared} LINE TEMİZLENDİ</color>\n" : "") +
                    (sb.ComboBonus > 0 ? $"<color=purple>+{sb.ComboBonus:N0} KOMBO {sb.ComboCount}</color>\n" : "") +
                    (sb.RiskBonus > 0 ? $"<color=blue>+{sb.RiskBonus} {(sb.IsCornerBonus ? "KÖŞE" : "KENAR")} BONUSU</color>\n" : "") +
                    "━━━━━━━━━━━━━━\n" +
                    $"<b>TOPLAM +{sb.TotalGained:N0}</b>";
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
                if (owner.gameStatusText == null)
                    return;

                var sb = breakdown.Breakdown;
                if (sb.UsedGrace)
                {
                    owner.ShowTransientStatusMessage(TrEn("KOMBO KORUNDU – Hazırlık Hamlesi", "COMBO PRESERVED – Setup Move"), 1.5f);
                    return;
                }
                if (sb.ComboBroken)
                {
                    owner.ShowTransientStatusMessage(TrEn("KOMBO KIRILDI", "COMBO BROKEN"), 1.5f);
                    return;
                }

                if (breakdown.ScoreDelta <= 0)
                    return;

                string quality = ResolveMoveQualityLabel(breakdown);
                if (string.IsNullOrEmpty(quality))
                    return;

                owner.ShowTransientStatusMessage(quality, 1.1f);
            }
        }
    }
}
