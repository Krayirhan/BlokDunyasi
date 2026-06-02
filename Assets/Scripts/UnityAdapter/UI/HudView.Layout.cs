using UnityEngine;

namespace BlockPuzzle.UnityAdapter.UI
{
    public partial class HudView
    {
        /// <summary>
        /// Layout-only presenter for HUD safe-area and anchor placement.
        /// </summary>
        private static class HudLayoutPresenter
        {
            public static void ApplyResponsiveHudLayout(HudView owner, bool force)
            {
                if (!owner.applySafeAreaLayout)
                    return;

                if (!force && !owner.HasScreenChanged() && !owner.HasBannerInsetChanged())
                    return;

                if (owner.useSceneAnchoredHudLayout)
                {
                    owner.CacheScreenState();
                    return;
                }

                owner.StretchHudPanelToSafeArea();
                Vector2 bottomInsetPadding = owner.GetBottomInsetPadding();

                if (owner.currentScoreText != null)
                    owner.SetToTopLeft(owner.currentScoreText.rectTransform, owner.topPadding);

                if (owner.bestScoreText != null && !owner.keepBestScoreAtInspectorPosition)
                    owner.SetToTopLeft(owner.bestScoreText.rectTransform, new Vector2(owner.topPadding.x, owner.topPadding.y + 58f));

                owner.RestoreBestScoreTextPosition();

                if (owner.targetProgressText != null)
                    owner.SetToTopCenter(owner.targetProgressText.rectTransform, new Vector2(0f, owner.topPadding.y + 2f));

                if (owner.comboText != null)
                    owner.SetToTopRight(owner.comboText.rectTransform, owner.comboTopPadding);

                if (owner.turnCountText != null)
                    owner.SetToBottomRight(owner.turnCountText.rectTransform, bottomInsetPadding);

                if (owner.gameStatusText != null)
                    owner.SetToBottomLeft(owner.gameStatusText.rectTransform, bottomInsetPadding);

                if (owner.autoPositionInGameMainMenuButton && owner.inGameMainMenuButton != null)
                {
                    var inGameMenuRect = owner.inGameMainMenuButton.transform as RectTransform;
                    owner.SetToTopRight(inGameMenuRect, owner.inGameMainMenuTopPadding);
                }

                if (owner.autoPositionThemeTestButton && owner.themeTestButton != null)
                {
                    var themeButtonRect = owner.themeTestButton.transform as RectTransform;
                    owner.SetToTopRight(themeButtonRect, owner.themeTestButtonTopPadding);
                    themeButtonRect.sizeDelta = owner.themeTestButtonSize;
                }

                owner.CacheScreenState();
            }
        }
    }
}
