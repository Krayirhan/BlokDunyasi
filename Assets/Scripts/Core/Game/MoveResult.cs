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
        public int TotalScore { get; private set; }
        public Int2 PlacementPosition { get; private set; }
        public int ShapeIndex { get; private set; }
        public string ErrorMessage { get; private set; }
        
        // Convenience properties
        public int ScoreDelta => ScoreResult.ScoreDelta;
        public int LinesCleared => ScoreResult.LinesCleared;
        public bool TriggersSpawn { get; private set; }
        
        private MoveResult(bool success, BoardState newBoardState, ScoreResult scoreResult, 
            int totalScore, Int2 placementPosition, int shapeIndex, string errorMessage = null, bool triggersSpawn = false)
        {
            IsSuccess = success;
            NewBoardState = newBoardState;
            ScoreResult = scoreResult;
            TotalScore = totalScore;
            PlacementPosition = placementPosition;
            ShapeIndex = shapeIndex;
            ErrorMessage = errorMessage;
            TriggersSpawn = triggersSpawn;
        }
        
        public static MoveResult CreateSuccess(int totalScore, ScoreResult scoreResult, bool triggersSpawn = false)
        {
            return new MoveResult(
                success: true,
                newBoardState: null,
                scoreResult: scoreResult,
                totalScore: totalScore,
                placementPosition: Int2.Zero,
                shapeIndex: 0,
                errorMessage: null,
                triggersSpawn: triggersSpawn);
        }
        
        public static MoveResult Failed(string errorMessage)
        {
            return new MoveResult(false, null, ScoreResult.Empty, 0, Int2.Zero, -1, errorMessage);
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
