// File: Core/Engine/GameState.cs
using System;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.Core.Shapes;

namespace BlockPuzzle.Core.Engine
{
    /// <summary>
    /// Complete game state including board, score, active blocks, and game status.
    /// Immutable structure for safe state management and undo/redo support.
    /// </summary>
    [Serializable]
    public class GameState
    {
        /// <summary>
        /// Current board state with placed blocks.
        /// </summary>
        public BoardState Board { get; private set; }
        
        /// <summary>
        /// Player's current score.
        /// </summary>
        public int Score { get; private set; }
        
        /// <summary>
        /// Currently active (unplaced) blocks available to player.
        /// </summary>
        public ActiveBlocks ActiveBlocks { get; private set; }
        
        /// <summary>
        /// Current combo state for score multipliers.
        /// </summary>
        public ComboState ComboState { get; private set; }
        
        /// <summary>
        /// Whether the game is over (no valid placements possible).
        /// </summary>
        public bool IsGameOver { get; private set; }
        
        /// <summary>
        /// Total number of moves made in this game.
        /// </summary>
        public int MoveCount { get; private set; }
        
        /// <summary>
        /// Total number of lines cleared in this game.
        /// </summary>
        public int TotalLinesCleared { get; private set; }
        
        /// <summary>
        /// Game start time for session tracking.
        /// </summary>
        public DateTime StartTime { get; private set; }
        
        /// <summary>
        /// Time of last move for analytics.
        /// </summary>
        public DateTime LastMoveTime { get; private set; }
        
        /// <summary>
        /// Current combo streak (convenience property).
        /// </summary>
        public int Combo => ComboState.CurrentStreak;
        
        /// <summary>
        /// Available shapes array (from ActiveBlocks).
        /// Returns 3 elements, null for empty slots.
        /// </summary>
        public ShapeDefinition[] AvailableShapes
        {
            get
            {
                // Always return 3 elements - one for each slot
                // null means slot is empty (block was placed)
                var shapes = new ShapeDefinition[3];
                
                for (int i = 0; i < 3; i++)
                {
                    if (ActiveBlocks.HasBlockAt(i))
                    {
                        var shapeId = ActiveBlocks.GetShapeId(i);
                        if (ShapeLibrary.TryGetShape(shapeId, out var shape))
                        {
                            shapes[i] = shape;
                        }
                        else
                        {
                            // BUG DETECTION: ShapeId exists in ActiveBlocks but not in ShapeLibrary!
                            // This causes the "2 blocks instead of 3" visual bug
                            System.Diagnostics.Debug.WriteLine($"[GameState.AvailableShapes] CRITICAL: Slot {i} has ShapeId {shapeId} but ShapeLibrary.TryGetShape returned false! This ShapeId is not registered in ShapeLibrary.");
                            // Fallback: Use Single block to prevent null slot
                            if (ShapeLibrary.TryGetShape(ShapeLibrary.Single, out var fallbackShape))
                            {
                                shapes[i] = fallbackShape;
                                System.Diagnostics.Debug.WriteLine($"[GameState.AvailableShapes] Using fallback Single block for slot {i}");
                            }
                        }
                    }
                    // else shapes[i] stays null (slot is empty - block was placed)
                }
                return shapes;
            }
        }
        
        /// <summary>
        /// Gets a specific available shape by index.
        /// </summary>
        public ShapeDefinition GetAvailableShape(int index)
        {
            if (!ActiveBlocks.HasBlockAt(index))
                return null;
            var shapeId = ActiveBlocks.GetShapeId(index);
            ShapeLibrary.TryGetShape(shapeId, out var shape);
            return shape;
        }
        
        public GameState(int boardWidth = 10, int boardHeight = 10)
        {
            Board = new BoardState(boardWidth, boardHeight);
            Score = 0;
            ActiveBlocks = new ActiveBlocks();
            ComboState = new ComboState();
            IsGameOver = false;
            MoveCount = 0;
            TotalLinesCleared = 0;
            StartTime = DateTime.Now;
            LastMoveTime = StartTime;
        }
        
        /// <summary>
        /// Creates a copy of this game state.
        /// </summary>
        /// <returns>Deep copy of current state</returns>
        public GameState Clone()
        {
            return new GameState(Board.Width, Board.Height)
            {
                Board = Board.Clone(),
                Score = Score,
                ActiveBlocks = ActiveBlocks.Clone(),
                ComboState = ComboState.Clone(),
                IsGameOver = IsGameOver,
                MoveCount = MoveCount,
                TotalLinesCleared = TotalLinesCleared,
                StartTime = StartTime,
                LastMoveTime = LastMoveTime
            };
        }
        
        /// <summary>
        /// Creates a new game state with updated board.
        /// </summary>
        /// <param name="newBoard">New board state</param>
        /// <returns>New GameState with updated board</returns>
        public GameState WithBoard(BoardState newBoard)
        {
            var newState = Clone();
            newState.Board = newBoard;
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state with updated score.
        /// </summary>
        /// <param name="newScore">New score value</param>
        /// <returns>New GameState with updated score</returns>
        public GameState WithScore(int newScore)
        {
            var newState = Clone();
            newState.Score = newScore;
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state with updated active blocks.
        /// </summary>
        /// <param name="newActiveBlocks">New active blocks</param>
        /// <returns>New GameState with updated active blocks</returns>
        public GameState WithActiveBlocks(ActiveBlocks newActiveBlocks)
        {
            var newState = Clone();
            newState.ActiveBlocks = newActiveBlocks;
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state with updated combo state.
        /// </summary>
        /// <param name="newComboState">New combo state</param>
        /// <returns>New GameState with updated combo state</returns>
        public GameState WithComboState(ComboState newComboState)
        {
            var newState = Clone();
            newState.ComboState = newComboState;
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state marking game as over.
        /// </summary>
        /// <returns>New GameState with game over flag set</returns>
        public GameState WithGameOver()
        {
            var newState = Clone();
            newState.IsGameOver = true;
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state with incremented move count.
        /// </summary>
        /// <returns>New GameState with incremented move count</returns>
        public GameState WithIncrementedMoveCount()
        {
            var newState = Clone();
            newState.MoveCount++;
            newState.LastMoveTime = DateTime.Now;
            return newState;
        }

        /// <summary>
        /// Creates a new game state with explicit move count.
        /// </summary>
        public GameState WithMoveCount(int moveCount)
        {
            var newState = Clone();
            newState.MoveCount = moveCount < 0 ? 0 : moveCount;
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state with updated lines cleared count.
        /// </summary>
        /// <param name="additionalLines">Number of lines to add to total</param>
        /// <returns>New GameState with updated lines cleared</returns>
        public GameState WithLinesCleared(int additionalLines)
        {
            var newState = Clone();
            newState.TotalLinesCleared += additionalLines;
            return newState;
        }

        /// <summary>
        /// Creates a new game state with explicit total lines cleared.
        /// </summary>
        public GameState WithTotalLinesCleared(int totalLinesCleared)
        {
            var newState = Clone();
            newState.TotalLinesCleared = totalLinesCleared < 0 ? 0 : totalLinesCleared;
            return newState;
        }

        /// <summary>
        /// Creates a new game state with explicit start time.
        /// </summary>
        public GameState WithStartTime(DateTime startTime)
        {
            var newState = Clone();
            newState.StartTime = startTime;
            return newState;
        }

        /// <summary>
        /// Creates a new game state with explicit last move time.
        /// </summary>
        public GameState WithLastMoveTime(DateTime lastMoveTime)
        {
            var newState = Clone();
            newState.LastMoveTime = lastMoveTime;
            return newState;
        }
        
        /// <summary>
        /// Gets the elapsed game time.
        /// </summary>
        /// <returns>Time elapsed since game start</returns>
        public TimeSpan GetElapsedTime()
        {
            return DateTime.Now - StartTime;
        }
        
        /// <summary>
        /// Gets the time since last move.
        /// </summary>
        /// <returns>Time since last move</returns>
        public TimeSpan GetTimeSinceLastMove()
        {
            return DateTime.Now - LastMoveTime;
        }
        
        public override string ToString()
        {
            return $"Score: {Score}, Moves: {MoveCount}, Lines: {TotalLinesCleared}, " +
                   $"ActiveBlocks: {ActiveBlocks.Count}, GameOver: {IsGameOver}";
        }
    }
}
