using UnityEngine;

namespace BlockPuzzle.Core.Meta.Cosmetics
{
    [CreateAssetMenu(fileName = "NewTheme", menuName = "BlockPuzzle/Meta/Cosmetic Theme")]
    public class CosmeticTheme : ScriptableObject
    {
        public string themeId;
        public string themeName;
        public Color backgroundColor;
        public Color primaryBlockColor;
        public Color secondaryBlockColor;
        public Color boardGridColor;
        
        [Header("Unlock Requirements")]
        public bool isPremium;
        public int costAmount;
        public string currencyId; // e.g. "theme_shard" or "coin"
    }
}