// File: Core/Board/LineDetectResult.cs
using System.Buffers;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Result of line detection operation, designed for allocation-free reuse.
    /// Uses internal buffers to avoid per-call allocations.
    /// </summary>
    public sealed class LineDetectResult
    {
        /// <summary>
        /// Array of full row indices. Length indicates actual count.
        /// Only indices [0..FullRowCount-1] are valid.
        /// </summary>
        public int[] FullRows { get; }
        
        /// <summary>
        /// Array of full column indices. Length indicates actual count.
        /// Only indices [0..FullColumnCount-1] are valid.
        /// </summary>
        public int[] FullColumns { get; }
        
        /// <summary>
        /// Number of full rows detected.
        /// </summary>
        public int FullRowCount { get; internal set; }
        
        /// <summary>
        /// Number of full columns detected.
        /// </summary>
        public int FullColumnCount { get; internal set; }
        
        /// <summary>
        /// Whether any full lines were detected.
        /// </summary>
        public bool HasFullLines => FullRowCount > 0 || FullColumnCount > 0;
        
        /// <summary>
        /// Creates a new result with pre-allocated buffers for the given board dimensions.
        /// This avoids per-detection allocations.
        /// </summary>
        /// <param name="maxWidth">Maximum board width (for column buffer)</param>
        /// <param name="maxHeight">Maximum board height (for row buffer)</param>
        internal LineDetectResult(int maxWidth, int maxHeight)
        {
            FullRows = new int[maxHeight];
            FullColumns = new int[maxWidth];
            FullRowCount = 0;
            FullColumnCount = 0;
        }
        
        /// <summary>
        /// Clears the result for reuse.
        /// </summary>
        internal void Clear()
        {
            FullRowCount = 0;
            FullColumnCount = 0;
        }
    }
}