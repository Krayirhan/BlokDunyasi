using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BlockPuzzle.Core.Meta.Cosmetics;
using BlockPuzzle.Core.Meta;

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
                    // TODO: Dispatch Event to switch current game theme
                }
                else
                {
                    Debug.LogWarning("Not enough currency to purchase theme.");
                }
            }
            else
            {
                Debug.Log($"Equipped Theme: {theme.themeName}");
                // TODO: Dispatch Event to switch current game theme
            }
        }
    }
}