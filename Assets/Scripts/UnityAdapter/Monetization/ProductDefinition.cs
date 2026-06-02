using UnityEngine;

namespace BlockPuzzle.Core.Monetization
{
    public enum ProductType
    {
        Consumable,
        NonConsumable
    }

    [CreateAssetMenu(fileName = "NewProduct", menuName = "BlockPuzzle/Monetization/Product Definition")]
    public class ProductDefinition : ScriptableObject
    {
        public string productId;
        public string productName;
        public string description;
        public string priceString; // e.g. "$1.99"
        
        public ProductType productType;

        [Header("Consumable Rewards")]
        public string rewardId; // e.g. "coin", "continue_token", "theme_shard"
        public int rewardAmount;

        [Header("Non-Consumable Entitlements")]
        public bool removesAds;
        public string unlockThemeId; // Unlocks a specific theme bypass
    }
}