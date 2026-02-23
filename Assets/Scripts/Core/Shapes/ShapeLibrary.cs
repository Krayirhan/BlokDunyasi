// File: Core/Shapes/ShapeLibrary.cs
using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Shapes
{
    /// <summary>
    /// Built-in library of common Blok Dünyası shapes.
    /// Provides deterministic access to standard shapes with unique IDs.
    /// </summary>
    public static class ShapeLibrary
    {
        private static readonly Dictionary<ShapeId, ShapeDefinition> _shapesById;
        private static readonly ShapeDefinition[] _allShapes;
        
        /// <summary>
        /// All available shapes in deterministic order.
        /// </summary>
        public static IReadOnlyList<ShapeDefinition> All => _allShapes;
        
        /// <summary>
        /// Static constructor to initialize the shape library.
        /// </summary>
        static ShapeLibrary()
        {
            var shapes = new List<ShapeDefinition>();
            
            // Singles
            shapes.Add(new ShapeDefinition(new ShapeId(1), "Single", new[] { new Int2(0, 0) }));
            
            // Lines - Horizontal
            shapes.Add(new ShapeDefinition(new ShapeId(2), "Line2H", new[] { new Int2(0, 0), new Int2(1, 0) }));
            shapes.Add(new ShapeDefinition(new ShapeId(3), "Line3H", new[] { new Int2(0, 0), new Int2(1, 0), new Int2(2, 0) }));
            shapes.Add(new ShapeDefinition(new ShapeId(4), "Line4H", new[] { new Int2(0, 0), new Int2(1, 0), new Int2(2, 0), new Int2(3, 0) }));
            
            // Lines - Vertical  
            shapes.Add(new ShapeDefinition(new ShapeId(5), "Line2V", new[] { new Int2(0, 0), new Int2(0, 1) }));
            shapes.Add(new ShapeDefinition(new ShapeId(6), "Line3V", new[] { new Int2(0, 0), new Int2(0, 1), new Int2(0, 2) }));
            shapes.Add(new ShapeDefinition(new ShapeId(7), "Line4V", new[] { new Int2(0, 0), new Int2(0, 1), new Int2(0, 2), new Int2(0, 3) }));
            
            // Squares
            shapes.Add(new ShapeDefinition(new ShapeId(8), "Square2x2", new[] { 
                new Int2(0, 0), new Int2(1, 0), new Int2(0, 1), new Int2(1, 1) }));
            shapes.Add(new ShapeDefinition(new ShapeId(9), "Square3x3", new[] { 
                new Int2(0, 0), new Int2(1, 0), new Int2(2, 0),
                new Int2(0, 1), new Int2(1, 1), new Int2(2, 1),
                new Int2(0, 2), new Int2(1, 2), new Int2(2, 2) }));
            
            // L-Shapes (Bottom-left origin)
            shapes.Add(new ShapeDefinition(new ShapeId(10), "L_Small", new[] { 
                new Int2(0, 0), new Int2(0, 1), new Int2(1, 0) }));
            shapes.Add(new ShapeDefinition(new ShapeId(11), "L_Medium", new[] { 
                new Int2(0, 0), new Int2(0, 1), new Int2(0, 2), new Int2(1, 0) }));
            shapes.Add(new ShapeDefinition(new ShapeId(12), "L_Large", new[] { 
                new Int2(0, 0), new Int2(0, 1), new Int2(0, 2), new Int2(1, 0), new Int2(2, 0) }));
            
            // Mirrored L-Shapes
            shapes.Add(new ShapeDefinition(new ShapeId(13), "L_Small_Mirror", new[] { 
                new Int2(0, 0), new Int2(1, 0), new Int2(1, 1) }));
            shapes.Add(new ShapeDefinition(new ShapeId(14), "L_Medium_Mirror", new[] { 
                new Int2(0, 0), new Int2(1, 0), new Int2(1, 1), new Int2(1, 2) }));
            shapes.Add(new ShapeDefinition(new ShapeId(15), "L_Large_Mirror", new[] { 
                new Int2(0, 0), new Int2(1, 0), new Int2(2, 0), new Int2(2, 1), new Int2(2, 2) }));
            
            // T-Shapes
            shapes.Add(new ShapeDefinition(new ShapeId(16), "T_Small", new[] { 
                new Int2(0, 0), new Int2(1, 0), new Int2(2, 0), new Int2(1, 1) }));
            shapes.Add(new ShapeDefinition(new ShapeId(17), "T_Large", new[] { 
                new Int2(0, 0), new Int2(1, 0), new Int2(2, 0), new Int2(1, 1), new Int2(1, 2) }));
            
            // Plus shapes
            shapes.Add(new ShapeDefinition(new ShapeId(18), "Plus_Small", new[] { 
                new Int2(0, 0), new Int2(1, 0), new Int2(-1, 0), new Int2(0, 1), new Int2(0, -1) }));
            
            // Z/S shapes
            shapes.Add(new ShapeDefinition(new ShapeId(19), "Z_Shape", new[] { 
                new Int2(0, 0), new Int2(1, 0), new Int2(1, 1), new Int2(2, 1) }));
            shapes.Add(new ShapeDefinition(new ShapeId(20), "S_Shape", new[] { 
                new Int2(0, 0), new Int2(1, 0), new Int2(-1, 1), new Int2(0, 1) }));
            
            // Corner piece
            shapes.Add(new ShapeDefinition(new ShapeId(21), "Corner", new[] { 
                new Int2(0, 0), new Int2(0, 1), new Int2(1, 1) }));
            
            // Create lookup dictionary
            _shapesById = new Dictionary<ShapeId, ShapeDefinition>();
            for (int i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                _shapesById[shape.Id] = shape;
            }
            
            _allShapes = shapes.ToArray();
        }
        
        /// <summary>
        /// Gets a shape by its ID.
        /// </summary>
        /// <param name="id">Shape ID to look up</param>
        /// <returns>Shape definition</returns>
        /// <exception cref="ArgumentException">If shape ID is not found</exception>
        public static ShapeDefinition GetById(ShapeId id)
        {
            if (_shapesById.TryGetValue(id, out ShapeDefinition shape))
            {
                return shape;
            }
            
            throw new ArgumentException($"Shape with ID {id} not found in library");
        }
        
        /// <summary>
        /// Tries to get a shape by its ID.
        /// </summary>
        /// <param name="id">Shape ID to look up</param>
        /// <param name="shape">Output shape definition if found</param>
        /// <returns>True if shape was found</returns>
        public static bool TryGetById(ShapeId id, out ShapeDefinition shape)
        {
            return _shapesById.TryGetValue(id, out shape);
        }
        
        /// <summary>
        /// Tries to get a shape by its ID (alias for TryGetById).
        /// </summary>
        public static bool TryGetShape(ShapeId id, out ShapeDefinition shape)
        {
            return TryGetById(id, out shape);
        }
        
        /// <summary>
        /// Gets all shape IDs in the library.
        /// </summary>
        public static ShapeId[] GetAllShapeIds()
        {
            var ids = new ShapeId[_allShapes.Length];
            for (int i = 0; i < _allShapes.Length; i++)
            {
                ids[i] = _allShapes[i].Id;
            }
            return ids;
        }
        
        /// <summary>
        /// Gets the single block shape ID.
        /// </summary>
        public static ShapeId Single => new ShapeId(1);
        
        /// <summary>
        /// Gets the number of shapes in the library.
        /// </summary>
        public static int Count => _allShapes.Length;
    }
}