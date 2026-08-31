using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BlockPuzzle.Core.Meta.Cosmetics;
using BlockPuzzle.Core.Meta;
using BlockPuzzle.UnityAdapter.UI;

namespace BlockPuzzle.UnityAdapter.Meta.UI
{
    public class CosmeticsPanel : MonoBehaviour
    {
        [SerializeField] private List<CosmeticTheme> availableThemes;
        [SerializeField] private Transform themeContainer;
        [SerializeField] private Button themeItemPrefab; // Can be expanded into a Custom Script 

        [Header("Currency Display")]
        [SerializeField] private Text coinText;
        [SerializeField] private Text shardText;

        private void OnEnable()
        {
            RefreshCurrencies();
            PopulateThemes();
            
            if (RewardInventory.Instance != null)
            {
                RewardInventory.Instance.OnInventoryUpdated += HandleInventoryUpdated;
            }
        }

        private void OnDisable()
        {
            if (RewardInventory.Instance != null)
            {
                RewardInventory.Instance.OnInventoryUpdated -= HandleInventoryUpdated;
            }
        }

        private void HandleInventoryUpdated(string rewardId, int amount)
        {
            RefreshCurrencies();
        }

        private void RefreshCurrencies()
        {
            if (RewardInventory.Instance != null)
            {
                coinText.text = $"Coins: {RewardInventory.Instance.GetAmount("coin")}";
                shardText.text = $"Shards: {RewardInventory.Instance.GetAmount("theme_shard")}";
            }
        }

        private void PopulateThemes()
        {
            foreach (Transform child in themeContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var theme in availableThemes)
            {
                Button newThemeBtn = Instantiate(themeItemPrefab, themeContainer);
                newThemeBtn.GetComponentInChildren<Text>().text = theme.themeName;

                // Add simple click listener to attempt to unlock/equip
                newThemeBtn.onClick.AddListener(() => OnThemeClicked(theme));
            }
        }

        private void OnThemeClicked(CosmeticTheme theme)
        {
            // Simple unlock logic
            if (theme.isPremium && theme.costAmount > 0)
            {
                if (RewardInventory.Instance != null && RewardInventory.Instance.ConsumeReward(theme.currencyId, theme.costAmount))
                {
                    Debug.Log($"Purchased Theme: {theme.themeName}");
                    EquipTheme(theme);
                }
                else
                {
                    Debug.LogWarning("Not enough currency to purchase theme.");
                }
            }
            else
            {
                Debug.Log($"Equipped Theme: {theme.themeName}");
                EquipTheme(theme);
            }
        }

        private static void EquipTheme(CosmeticTheme theme)
        {
            if (theme == null)
                return;

            if (!TryResolveThemeId(theme.themeId, theme.themeName, out int themeId))
            {
                Debug.LogWarning($"[CosmeticsPanel] Could not resolve theme ID for '{theme.themeName}'.");
                return;
            }

            // The gameplay scene owns the visual controller. Persist the choice
            // here so that controller applies the complete scene theme on load.
            UISettingsProfile.SetThemeId(themeId);
            UISettingsProfile.SetLastAutomaticThemeId(themeId);
            Debug.Log($"[CosmeticsPanel] Equipped theme {themeId}: {theme.themeName}");
        }

        private static bool TryResolveThemeId(string rawId, string themeName, out int themeId)
        {
            if (int.TryParse(rawId, out themeId))
            {
                // Accept both the internal 0-based IDs and human-facing 1-based IDs.
                if (themeId >= 1 && themeId <= 4 && !rawId.TrimStart().StartsWith("0"))
                    themeId--;
                return themeId >= UISettingsProfile.ThemeClassic && themeId <= UISettingsProfile.ThemeWood;
            }

            string value = $"{rawId} {themeName}".ToLowerInvariant();
            if (value.Contains("classic") || value.Contains("theme 1")) themeId = UISettingsProfile.ThemeClassic;
            else if (value.Contains("neon") || value.Contains("teal") || value.Contains("night") || value.Contains("theme 2")) themeId = UISettingsProfile.ThemeNight;
            else if (value.Contains("zen") || value.Contains("doğa") || value.Contains("doga") || value.Contains("theme 3")) themeId = UISettingsProfile.ThemeVivid;
            else if (value.Contains("wood") || value.Contains("ahşap") || value.Contains("ahsap") || value.Contains("theme 4")) themeId = UISettingsProfile.ThemeWood;
            else { themeId = UISettingsProfile.ThemeClassic; return false; }
            return true;
        }
    }
}
