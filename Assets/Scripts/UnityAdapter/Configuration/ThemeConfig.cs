using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Configuration
{
    [CreateAssetMenu(fileName = "NewThemeConfig", menuName = "BlockPuzzle/Theme Config")]
    public class ThemeConfig : ScriptableObject
    {
        [Header("Grid Layout")]
        public Vector2 GridOffset = Vector2.zero;
        [Range(0f, 0.5f)] public float CellSpacing = 0.05f;
        [Range(0.2f, 2.0f)] public float CellSize = 0.95f;
        public Sprite CustomCellSprite; // "Yuvarla" (rounding) etc. can be done by providing a rounded sprite here.

        [Header("Empty Cells (Background Grid)")]
        public Color EmptyCellColor = new Color(0.12f, 0.15f, 0.25f, 1f);
        public bool UseGlowForEmptyCells = false;
        public Color EmptyCellGlowColor = new Color(0.12f, 0.15f, 0.25f, 0.5f);

        [Header("Board Backdrop")]
        public bool ShowBoardBackdrop = true;
        public Sprite CustomBackdropSprite; // Allows "yuvarla" (rounding) via sprite.
        public Color BoardBackdropColor = new Color(0.06f, 0.1f, 0.2f, 0.55f);
        [Range(-2f, 5f)] public float BoardBackdropPaddingInCells = 0.35f;
        public bool ShowBoardBackdropBorder = true;
        public Color BoardBackdropBorderColor = new Color(0.75f, 0.9f, 1f, 0.12f);
        [Range(0f, 1f)] public float BoardBackdropBorderThicknessInCells = 0.12f;
        public Vector2 BackdropOffset = Vector2.zero; // "Kaydır" (offset) specifically for backdrop.

        [Header("Sorting")]
        [Range(-20, 20)] public int BoardBackdropSortingOrder = -2;
    }
}
