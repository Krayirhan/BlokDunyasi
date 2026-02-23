using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Rules;

namespace BlockPuzzle.Core.Game
{
    /// <summary>
    /// Result of a shape placement attempt.
    /// </summary>
    public class MoveResult
    {
        public bool IsSuccess { get; private set; }
        public bool Success => IsSuccess; // Alias for compatibility
        public BoardState NewBoardState { get; private set; }
        public ScoreResult ScoreResult { get; private set; }
        public Int2 PlacementPosition { get; private set; }
        public int ShapeIndex { get; private set; }
        public string ErrorMessage { get; private set; }
        
        // Convenience properties
        public int LinesCleared => ScoreResult.LinesCleared;
        public bool TriggersSpawn { get; private set; }
        
        private MoveResult(bool success, BoardState newBoardState, ScoreResult scoreResult, 
            Int2 placementPosition, int shapeIndex, string errorMessage = null, bool triggersSpawn = false)
        {
            IsSuccess = success;
            NewBoardState = newBoardState;
            ScoreResult = scoreResult;
            PlacementPosition = placementPosition;
            ShapeIndex = shapeIndex;
            ErrorMessage = errorMessage;
            TriggersSpawn = triggersSpawn;
        }
        
        public static MoveResult CreateSuccess(int score, int linesCleared, bool triggersSpawn = false)
        {
            return new MoveResult(true, null, new ScoreResult(score, linesCleared, 0, 1.0f, score, 1.0f), Int2.Zero, 0, null, triggersSpawn);
        }
        
        public static MoveResult Failed(string errorMessage)
        {
            return new MoveResult(false, null, ScoreResult.Empty, Int2.Zero, -1, errorMessage);
        }
        
        // Static result instances for common failure cases
        public static readonly MoveResult GameOverResult = Failed("Game is over");
        public static readonly MoveResult InvalidBlockIndex = Failed("Invalid block index");
        public static readonly MoveResult InvalidShape = Failed("Invalid shape");
        public static readonly MoveResult OutOfBounds = Failed("Placement is out of bounds");
        public static readonly MoveResult CellsOccupied = Failed("Cells are already occupied");
        public static readonly MoveResult InvalidPlacement = Failed("Invalid placement");
    }
}