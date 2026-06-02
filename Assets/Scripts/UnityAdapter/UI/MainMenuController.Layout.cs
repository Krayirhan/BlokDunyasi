using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UnityAdapter.UI
{
    public partial class MainMenuController
    {
        /// <summary>
        /// Layout-only presenter for MainMenuController. The facade keeps flow and scene bindings stable.
        /// </summary>
        private static class MainMenuLayoutPresenter
        {
            public static void ApplyResponsiveMenuLayout(MainMenuController owner, bool force)
            {
                if (!owner.applyResponsiveLayout)
                    return;

                owner.ResolveRuntimeReferences(allowCreate: false);

                if (!force && !owner.HasScreenMetricsChanged())
                    return;

                RectTransform safeRoot = owner.mainMenuSafeAreaRoot;
                if (safeRoot == null)
                    return;

                if (owner.autoDetectDevice)
                    owner._currentProfile = owner.DetectDeviceProfile();

                if (owner._currentProfile == null)
                    owner._currentProfile = owner.defaultProfile;

                if (owner._currentProfile == null)
                {
                    Debug.LogWarning("[MainMenuController] No device profile configured!");
                    return;
                }

                ApplySafeAreaToRoot(safeRoot);
                owner.EnsureResponsiveLayoutHierarchy(safeRoot);
                if (owner._responsiveFrameRoot == null || owner._responsiveContentRoot == null)
                    return;

                float safeWidth = Mathf.Max(1f, safeRoot.rect.width);
                float safeHeight = Mathf.Max(1f, safeRoot.rect.height);
                Vector4 edgePadding = owner._currentProfile.CalculateEdgePadding(safeWidth, safeHeight);
                float horizontalPadding = edgePadding.x;
                float topPadding = edgePadding.y;
                float bottomPadding = edgePadding.w;

                StretchToParent(owner._responsiveFrameRoot, new Vector2(horizontalPadding, bottomPadding), new Vector2(-horizontalPadding, -topPadding));

                owner._responsiveContentRoot.anchorMin = new Vector2(0.5f, 1f);
                owner._responsiveContentRoot.anchorMax = new Vector2(0.5f, 1f);
                owner._responsiveContentRoot.pivot = new Vector2(0.5f, 1f);
                owner._responsiveContentRoot.anchoredPosition = Vector2.zero;
                owner._responsiveContentRoot.sizeDelta = Vector2.zero;
                owner._responsiveContentRoot.localScale = Vector3.one;

                RectTransform activeLogo = owner.GetActiveLogoRect();
                if (owner.logoTurkishObject != null)
                {
                    var rect = owner.logoTurkishObject.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        EnsureEffectiveSize(rect);
                        SetRectAsCenteredChild(rect, owner._logoSlot, false);
                    }
                }
                if (owner.logoEnglishObject != null)
                {
                    var rect = owner.logoEnglishObject.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        EnsureEffectiveSize(rect);
                        SetRectAsCenteredChild(rect, owner._logoSlot, false);
                    }
                }
                if (owner.logoKoreanObject != null)
                {
                    var rect = owner.logoKoreanObject.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        EnsureEffectiveSize(rect);
                        SetRectAsCenteredChild(rect, owner._logoSlot, false);
                    }
                }

                UpdateSlotPreferredSize(owner._logoSlot, activeLogo, owner._currentProfile.logoSlotPaddingH, owner._currentProfile.logoSlotPaddingV);

                if (owner.bestScoreBadgeRoot != null)
                {
                    EnsureEffectiveSize(owner.bestScoreBadgeRoot);
                    SetRectAsCenteredChild(owner.bestScoreBadgeRoot, owner._badgeSlot, false);
                    UpdateSlotPreferredSize(owner._badgeSlot, owner.bestScoreBadgeRoot, owner._currentProfile.badgeSlotPaddingH, owner._currentProfile.badgeSlotPaddingV);
                }

                float playSlotWidth = 0f;
                float playSlotHeight = 0f;
                if (owner.playButtonGlowRig != null)
                {
                    EnsureEffectiveSize(owner.playButtonGlowRig);
                    SetRectAsCenteredChild(owner.playButtonGlowRig, owner._playSlot, false);
                    owner.playButtonGlowRig.SetAsFirstSibling();
                    playSlotWidth = Mathf.Max(playSlotWidth, owner.playButtonGlowRig.sizeDelta.x);
                    playSlotHeight = Mathf.Max(playSlotHeight, owner.playButtonGlowRig.sizeDelta.y);
                }

                if (owner.playButton != null)
                {
                    LayoutManagedButton(owner.playButton, owner._playSlot, owner.playButtonHitZone);
                    var playRect = owner.playButton.transform as RectTransform;
                    if (playRect != null)
                    {
                        playSlotWidth = Mathf.Max(playSlotWidth, playRect.sizeDelta.x);
                        playSlotHeight = Mathf.Max(playSlotHeight, playRect.sizeDelta.y);
                    }
                }

                if (playSlotWidth > 0f && playSlotHeight > 0f)
                {
                    UpdateLayoutElement(
                        owner._playSlot,
                        playSlotWidth + owner._currentProfile.playSlotPaddingH,
                        playSlotHeight + owner._currentProfile.playSlotPaddingV);
                }

                LayoutManagedButton(owner.continueButton, owner._secondaryButtonsRow, owner.continueButtonHitZone);
                LayoutManagedButton(owner.scoresButton, owner._secondaryButtonsRow, owner.scoresButtonHitZone);
                UpdateRowPreferredSize(owner._secondaryButtonsRow, owner._currentProfile.horizontalSpacing, owner.continueButton, owner.scoresButton);

                if (owner.authPanelRoot != null)
                {
                    EnsureEffectiveSize(owner.authPanelRoot);
                    SetRectAsCenteredChild(owner.authPanelRoot, owner._authSlot, false);
                    UpdateSlotPreferredSize(owner._authSlot, owner.authPanelRoot, owner._currentProfile.authSlotPaddingH, owner._currentProfile.authSlotPaddingV);
                }

                if (owner.settingsButton != null)
                {
                    var settingsRect = owner.settingsButton.transform as RectTransform;
                    if (settingsRect != null)
                    {
                        EnsureEffectiveSize(settingsRect);
                        SetRectTopRight(settingsRect, owner._responsiveFrameRoot, Mathf.Clamp(safeWidth * 0.02f, 12f, 24f), 0f);
                    }
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(owner._responsiveContentRoot);
                float preferredWidth = LayoutUtility.GetPreferredWidth(owner._responsiveContentRoot);
                float preferredHeight = LayoutUtility.GetPreferredHeight(owner._responsiveContentRoot);
                float availableHeight = Mathf.Max(1f, owner._responsiveFrameRoot.rect.height);
                float availableWidth = Mathf.Max(1f, owner._responsiveFrameRoot.rect.width);
                float widthFitScale = availableWidth / Mathf.Max(1f, preferredWidth);
                float heightFitScale = availableHeight / Mathf.Max(1f, preferredHeight);
                float fitScale = Mathf.Clamp(Mathf.Min(1f, widthFitScale, heightFitScale), owner._currentProfile.minScale, owner._currentProfile.maxScale);
                owner._responsiveContentRoot.localScale = Vector3.one * fitScale;

                if (owner.showLayoutDebug)
                    owner.DebugLayoutInfo(safeWidth, safeHeight, fitScale);

                var loginUiManager = FindFirstObjectByType<LoginUIManager>(FindObjectsInactive.Include);
                if (loginUiManager != null)
                    loginUiManager.SendMessage("UpdateUI", SendMessageOptions.DontRequireReceiver);

                owner.CacheScreenMetrics();
            }
        }
    }
}
