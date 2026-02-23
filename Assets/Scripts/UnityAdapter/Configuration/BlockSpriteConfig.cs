// =============================================================================
// BLOK DÜNYASI - SPRITE CONFIGURATION
// Centralized sprite management system
// =============================================================================

using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Configuration
{
    /// <summary>
    /// ScriptableObject for centralized sprite management.
    /// Create via: Assets → Create → Blok Dünyası → Sprite Config
    /// </summary>
    [CreateAssetMenu(fileName = "BlockSpriteConfig", menuName = "Blok Dünyası/Sprite Config")]
    public class BlockSpriteConfig : ScriptableObject
    {
        [Header("🎨 BLOCK SPRITES")]
        [SerializeField] private Sprite[] blockSprites = new Sprite[8];
        
        [Header("🔲 GRID SPRITES")]
        [SerializeField] private Sprite emptyCellSprite;
        [SerializeField] private Sprite filledCellSprite;
        
        [Header("🎯 PREVIEW SPRITES")]
        [SerializeField] private Sprite previewValidSprite;
        [SerializeField] private Sprite previewInvalidSprite;
        
        [Header("🔧 PREFABS")]
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private GameObject blockCellPrefab;
        
        /// <summary>
        /// Gets block sprite by index (0-7).
        /// </summary>
        public Sprite GetBlockSprite(int index)
        {
            if (blockSprites == null || blockSprites.Length == 0)
                return null;
                
            return blockSprites[index % blockSprites.Length];
        }
        
        /// <summary>
        /// Gets block sprite by ColorId (1-based).
        /// ColorId 1 -> index 0, ColorId 2 -> index 1, etc.
        /// </summary>
        public Sprite GetBlockSpriteByColorId(int colorId)
        {
            if (blockSprites == null || blockSprites.Length == 0)
                return null;
            
            // ColorId is 1-based, convert to 0-based index
            int index = (colorId - 1) % blockSprites.Length;
            return blockSprites[index];
        }
        
        /// <summary>
        /// Gets random block sprite.
        /// </summary>
        public Sprite GetRandomBlockSprite()
        {
            if (blockSprites == null || blockSprites.Length == 0)
                return null;
                
            int randomIndex = Random.Range(0, blockSprites.Length);
            return blockSprites[randomIndex];
        }
        
        /// <summary>
        /// Gets empty cell sprite.
        /// </summary>
        public Sprite EmptyCellSprite => emptyCellSprite;
        
        /// <summary>
        /// Gets filled cell sprite (fallback if no specific sprite).
        /// </summary>
        public Sprite FilledCellSprite => filledCellSprite;
        
        /// <summary>
        /// Gets preview sprite based on validity.
        /// </summary>
        public Sprite GetPreviewSprite(bool isValid)
        {
            return isValid ? previewValidSprite : previewInvalidSprite;
        }
        
        /// <summary>
        /// Gets cell prefab for grid.
        /// </summary>
        public GameObject CellPrefab => cellPrefab;
        
        /// <summary>
        /// Gets block cell prefab for draggable blocks.
        /// </summary>
        public GameObject BlockCellPrefab => blockCellPrefab;
        
        /// <summary>
        /// Validates configuration in editor.
        /// </summary>
        private void OnValidate()
        {
            if (blockSprites != null)
            {
                for (int i = 0; i < blockSprites.Length; i++)
                {
                    if (blockSprites[i] == null)
                    {
                        Debug.LogWarning($"[BlockSpriteConfig] Block sprite at index {i} is null");
                    }
                }
            }
            
            if (cellPrefab == null)
            {
                Debug.LogWarning("[BlockSpriteConfig] Cell prefab is null");
            }
                
            if (blockCellPrefab == null)
            {
                Debug.LogWarning("[BlockSpriteConfig] Block cell prefab is null");
            }
        }
        
        /// <summary>
        /// Creates default configuration for testing.
        /// </summary>
        [ContextMenu("Create Default Config")]
        private void CreateDefaultConfig()
        {
            Debug.Log("[BlockSpriteConfig] Creating default configuration...");
            
            // Bu sadece test için - gerçek sprite'ları manuel atayacaksınız
            if (blockSprites == null || blockSprites.Length != 8)
            {
                blockSprites = new Sprite[8];
            }
        }
    }
}