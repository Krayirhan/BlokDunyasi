using UnityEngine;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Configuration;
using BlockPuzzle.UnityAdapter.Components;

namespace BlockPuzzle.UnityAdapter.Grid
{
    /// <summary>
    /// Basit, temiz grid sistemi. Tek sorumluluk: Grid görselleştirme ve world<->grid dönüşümü.
    /// </summary>
    public class SimpleGridView : MonoBehaviour
    {
        [Header("📁 CONFIGURATION")]
        [SerializeField] private BlockSpriteConfig spriteConfig;

        [Header("⚙️ GRID SETTINGS")]
        [SerializeField] [Range(0.3f, 1.0f)] private float cellSize = 0.5f;
        [SerializeField] [Range(0f, 0.2f)] private float cellSpacing = 0f;

        [Header("🎨 COLORS")]
        [SerializeField] private Color emptyCellColor = new Color(0.12f, 0.15f, 0.25f, 1f);
        [SerializeField] private Color filledCellColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        [Header("🌈 BLOCK COLOR PALETTE (Match SimpleBlock)")]
        [SerializeField] private Color[] blockColors =
        {
            new Color(0.9f, 0.3f, 0.8f),
            new Color(0.5f, 0.3f, 0.9f),
            new Color(0.2f, 0.7f, 1f),
            new Color(1f, 0.5f, 0.2f),
            new Color(0.3f, 0.8f, 0.4f),
            new Color(1f, 0.2f, 0.2f),
            new Color(0.9f, 0.8f, 0.1f),
            new Color(0.7f, 0.4f, 1f)
        };

        private SimpleCell[,] _cells;
        private int _width;
        private int _height;
        private bool _isInitialized;

        public float CellSize => cellSize;
        public float CellSpacing => cellSpacing;
        public float TotalCellSize => cellSize + cellSpacing;
        public int Width => _width;
        public int Height => _height;

        public Vector3 GetWorldPosition(int x, int y)
        {
            float totalSize = cellSize + cellSpacing;
            return transform.position + new Vector3(
                (x - (_width - 1) * 0.5f) * totalSize,
                ((_height - 1) * 0.5f - y) * totalSize,
                0
            );
        }

        public bool GetGridPosition(Vector3 worldPos, out int x, out int y)
        {
            Vector3 localPos = worldPos - transform.position;
            float totalSize = cellSize + cellSpacing;

            x = Mathf.RoundToInt(localPos.x / totalSize + (_width - 1) * 0.5f);
            y = Mathf.RoundToInt((_height - 1) * 0.5f - localPos.y / totalSize);

            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        private void Start()
        {
            GameBootstrap.OnBoardChanged += OnBoardChanged;
        }

        private void OnDestroy()
        {
            GameBootstrap.OnBoardChanged -= OnBoardChanged;
        }

        private void OnBoardChanged(BoardState boardState, Int2[] clearedPositions, int linesCleared)
        {
            if (!_isInitialized)
            {
                InitializeGrid(boardState);
            }

            UpdateGrid(boardState);
        }

        private void InitializeGrid(BoardState boardState)
        {
            _width = boardState.Width;
            _height = boardState.Height;
            _cells = new SimpleCell[_width, _height];

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    CreateCellAt(x, y);
                }
            }

            _isInitialized = true;
            Debug.Log($"[SimpleGridView] Grid initialized: {_width}x{_height}");
        }

        private void CreateCellAt(int x, int y)
        {
            var cellObj = new GameObject($"Cell_{x}_{y}");
            cellObj.transform.SetParent(transform);
            cellObj.transform.position = GetWorldPosition(x, y);

            var spriteRenderer = cellObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 1;

            var cell = cellObj.AddComponent<SimpleCell>();
            cell.SetSortingOrder(1);

            Sprite emptySprite = spriteConfig?.EmptyCellSprite;
            if (emptySprite != null)
            {
                cell.SetSprite(emptySprite);
                cell.SetColor(Color.white);

                float spriteWorldSize = emptySprite.bounds.size.x;
                if (spriteWorldSize > 0)
                {
                    float scale = cellSize / spriteWorldSize;
                    cellObj.transform.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    cellObj.transform.localScale = Vector3.one * cellSize;
                }
            }
            else
            {
                Sprite defaultSprite = CreateDefaultSquareSprite();
                cell.SetSprite(defaultSprite);
                cell.SetColor(emptyCellColor);
                cellObj.transform.localScale = new Vector3(cellSize, cellSize, 1f);
            }

            _cells[x, y] = cell;
        }

        private Sprite CreateDefaultSquareSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.white;

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void UpdateGrid(BoardState boardState)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_cells[x, y] == null) continue;

                    bool isFilled = boardState.IsOccupied(x, y);
                    var cellTransform = _cells[x, y].transform;

                    if (isFilled)
                    {
                        var cellState = boardState.GetCell(x, y);
                        Color cellColor = GetBlockColor(cellState.ColorId);

                        Sprite filledSprite = spriteConfig?.GetBlockSpriteByColorId(cellState.ColorId);
                        if (filledSprite != null)
                        {
                            _cells[x, y].SetSprite(filledSprite);
                            _cells[x, y].SetColor(cellColor);

                            float spriteWorldSize = filledSprite.bounds.size.x;
                            if (spriteWorldSize > 0)
                            {
                                float scale = cellSize / spriteWorldSize;
                                cellTransform.localScale = new Vector3(scale, scale, 1f);
                            }
                        }
                        else
                        {
                            _cells[x, y].SetColor(cellColor);
                        }
                    }
                    else
                    {
                        Sprite emptySprite = spriteConfig?.EmptyCellSprite;
                        if (emptySprite != null)
                        {
                            _cells[x, y].SetSprite(emptySprite);
                            _cells[x, y].SetColor(Color.white);

                            float spriteWorldSize = emptySprite.bounds.size.x;
                            if (spriteWorldSize > 0)
                            {
                                float scale = cellSize / spriteWorldSize;
                                cellTransform.localScale = new Vector3(scale, scale, 1f);
                            }
                        }
                        else
                        {
                            _cells[x, y].SetColor(emptyCellColor);
                        }
                    }
                }
            }
        }

        private Color GetBlockColor(int colorId)
        {
            if (blockColors == null || blockColors.Length == 0)
                return new Color(1f, 0.4f, 0.8f);

            int colorIndex = (colorId - 1) % blockColors.Length;
            return blockColors[colorIndex];
        }
    }
}
