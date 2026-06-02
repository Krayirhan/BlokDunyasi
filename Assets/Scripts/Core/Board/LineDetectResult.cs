namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Mutable result buffer for line detection.
    /// <para>
    /// This type is reusable and caller-owned. <see cref="FullRows"/> and <see cref="FullColumns"/>
    /// are backing buffers; only indices in the ranges <c>[0..FullRowCount-1]</c> and
    /// <c>[0..FullColumnCount-1]</c> are valid after detection.
    /// </para>
    /// <para>
    /// For allocation-free usage, create one instance and pass it to
    /// <see cref="LineDetector.DetectFullLines(BoardState, LineDetectResult)"/>.
    /// The convenience overload <see cref="LineDetector.DetectFullLines(BoardState)"/> is safe but allocates.
    /// </para>
    /// </summary>
    public sealed class LineDetectResult
    {
        /// <summary>
        /// Buffer containing full row indices.
        /// Only indices [0..FullRowCount-1] are valid.
        /// </summary>
        public int[] FullRows { get; private set; }

        /// <summary>
        /// Buffer containing full column indices.
        /// Only indices [0..FullColumnCount-1] are valid.
        /// </summary>
        public int[] FullColumns { get; private set; }

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
        /// Creates a reusable result with buffers sized for the given board dimensions.
        /// </summary>
        /// <param name="maxWidth">Maximum board width (for column buffer)</param>
        /// <param name="maxHeight">Maximum board height (for row buffer)</param>
        public LineDetectResult(int maxWidth, int maxHeight)
        {
            if (maxWidth < 0)
                throw new System.ArgumentOutOfRangeException(nameof(maxWidth));
            if (maxHeight < 0)
                throw new System.ArgumentOutOfRangeException(nameof(maxHeight));

            FullRows = new int[maxHeight];
            FullColumns = new int[maxWidth];
            FullRowCount = 0;
            FullColumnCount = 0;
        }

        /// <summary>
        /// Ensures the internal buffers can hold a board of the requested size.
        /// </summary>
        internal void EnsureCapacity(int requiredWidth, int requiredHeight)
        {
            if (requiredWidth < 0)
                throw new System.ArgumentOutOfRangeException(nameof(requiredWidth));
            if (requiredHeight < 0)
                throw new System.ArgumentOutOfRangeException(nameof(requiredHeight));

            if (FullRows.Length < requiredHeight)
                FullRows = new int[requiredHeight];

            if (FullColumns.Length < requiredWidth)
                FullColumns = new int[requiredWidth];
        }

        /// <summary>
        /// Clears counts so the result can be reused safely.
        /// Buffer contents beyond the active count are unspecified.
        /// </summary>
        internal void Clear()
        {
            FullRowCount = 0;
            FullColumnCount = 0;
        }
    }
}
