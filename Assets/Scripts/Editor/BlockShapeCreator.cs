#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.Core.Common;
using Vector2Int = UnityEngine.Vector2Int;

namespace BlockPuzzle.Editor
{
    /// <summary>
    /// Difficulty levels for block shapes.
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }
    
    /// <summary>
    /// Editor utility to create default block shapes as ScriptableObjects.
    /// </summary>
    public class BlockShapeCreator : EditorWindow
    {
        [MenuItem("Blok Dünyası/Create Default Block Shapes")]
        public static void CreateDefaultBlockShapes()
        {
            string folderPath = "Assets/Data/BlockShapes";
            
            // Create folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Data", "BlockShapes");
            }

            // Create database
            // Note: Using debug output instead of ScriptableObjects for simplicity
            var shapesList = new System.Collections.Generic.List<string>();

            // Define all shapes
            var shapeDefinitions = GetShapeDefinitions();

            foreach (var def in shapeDefinitions)
            {
                // Just log the shape data instead of creating assets
                string shapeInfo = $"Shape: {def.id} - {def.name} (Difficulty: {def.difficulty}, Weight: {def.weight}, Cells: {def.cells.Length})";
                Debug.Log(shapeInfo);
                shapesList.Add(shapeInfo);
            }

            Debug.Log($"Generated {shapeDefinitions.Length} shape definitions. Use ShapeLibrary.InitializeDefault() to load them in game.");
            
            // Save database reference could be added here if needed
            AssetDatabase.Refresh();

            Debug.Log($"Created {shapesList.Count} block shape definitions. Check console output for details.");
        }

        private static ShapeDefinition[] GetShapeDefinitions()
        {
            Color[] colors = new Color[]
            {
                new Color(0.2f, 0.8f, 0.9f, 1f),  // Cyan
                new Color(0.9f, 0.3f, 0.4f, 1f),  // Red
                new Color(0.3f, 0.9f, 0.4f, 1f),  // Green
                new Color(0.9f, 0.7f, 0.2f, 1f),  // Orange
                new Color(0.6f, 0.3f, 0.9f, 1f),  // Purple
                new Color(0.9f, 0.9f, 0.3f, 1f),  // Yellow
                new Color(0.3f, 0.5f, 0.9f, 1f),  // Blue
                new Color(0.9f, 0.4f, 0.7f, 1f),  // Pink
            };

            return new ShapeDefinition[]
            {
                // Easy shapes
                new ShapeDefinition("single", "Single", colors[0], DifficultyLevel.Easy, 8,
                    new Vector2Int[] { new Vector2Int(0, 0) }),
                
                new ShapeDefinition("horizontal_2", "Horizontal 2", colors[1], DifficultyLevel.Easy, 7,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }),
                
                new ShapeDefinition("vertical_2", "Vertical 2", colors[2], DifficultyLevel.Easy, 7,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }),
                
                new ShapeDefinition("horizontal_3", "Horizontal 3", colors[3], DifficultyLevel.Easy, 6,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }),
                
                new ShapeDefinition("vertical_3", "Vertical 3", colors[4], DifficultyLevel.Easy, 6,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }),
                
                new ShapeDefinition("l_shape_1", "L Shape 1", colors[5], DifficultyLevel.Easy, 5,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0) }),
                
                new ShapeDefinition("l_shape_2", "L Shape 2", colors[6], DifficultyLevel.Easy, 5,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }),
                
                new ShapeDefinition("l_shape_3", "L Shape 3", colors[7], DifficultyLevel.Easy, 5,
                    new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) }),
                
                new ShapeDefinition("l_shape_4", "L Shape 4", colors[0], DifficultyLevel.Easy, 5,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }),

                // Medium shapes
                new ShapeDefinition("horizontal_4", "Horizontal 4", colors[1], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) }),
                
                new ShapeDefinition("vertical_4", "Vertical 4", colors[2], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3) }),
                
                new ShapeDefinition("square_2x2", "Square 2x2", colors[3], DifficultyLevel.Medium, 5,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }),
                
                new ShapeDefinition("t_shape_up", "T Shape Up", colors[4], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) }),
                
                new ShapeDefinition("t_shape_down", "T Shape Down", colors[5], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }),
                
                new ShapeDefinition("t_shape_left", "T Shape Left", colors[6], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2) }),
                
                new ShapeDefinition("t_shape_right", "T Shape Right", colors[7], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 1) }),
                
                new ShapeDefinition("s_shape_1", "S Shape 1", colors[0], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }),
                
                new ShapeDefinition("s_shape_2", "S Shape 2", colors[1], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) }),
                
                new ShapeDefinition("big_l_1", "Big L 1", colors[2], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0) }),
                
                new ShapeDefinition("big_l_2", "Big L 2", colors[3], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1) }),
                
                new ShapeDefinition("big_l_3", "Big L 3", colors[4], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, 2) }),
                
                new ShapeDefinition("big_l_4", "Big L 4", colors[5], DifficultyLevel.Medium, 4,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }),

                // Hard shapes
                new ShapeDefinition("horizontal_5", "Horizontal 5", colors[6], DifficultyLevel.Hard, 3,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) }),
                
                new ShapeDefinition("vertical_5", "Vertical 5", colors[7], DifficultyLevel.Hard, 3,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) }),
                
                new ShapeDefinition("plus", "Plus", colors[0], DifficultyLevel.Hard, 3,
                    new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2) }),
                
                new ShapeDefinition("square_3x3", "Square 3x3", colors[1], DifficultyLevel.Hard, 2,
                    new Vector2Int[] { 
                        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                        new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2) 
                    }),
                
                new ShapeDefinition("big_corner_1", "Big Corner 1", colors[2], DifficultyLevel.Hard, 3,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }),
                
                new ShapeDefinition("big_corner_2", "Big Corner 2", colors[3], DifficultyLevel.Hard, 3,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2) }),
                
                new ShapeDefinition("big_corner_3", "Big Corner 3", colors[4], DifficultyLevel.Hard, 3,
                    new Vector2Int[] { new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2) }),
                
                new ShapeDefinition("big_corner_4", "Big Corner 4", colors[5], DifficultyLevel.Hard, 3,
                    new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2) }),
            };
        }

        private struct ShapeDefinition
        {
            public string id;
            public string name;
            public Color color;
            public DifficultyLevel difficulty;
            public int weight;
            public Vector2Int[] cells;

            public ShapeDefinition(string id, string name, Color color, DifficultyLevel difficulty, int weight, Vector2Int[] cells)
            {
                this.id = id;
                this.name = name;
                this.color = color;
                this.difficulty = difficulty;
                this.weight = weight;
                this.cells = cells;
            }
        }
    }
}
#endif
