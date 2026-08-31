// File: Core/Engine/GameState.cs
using System;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Engine
{
    /// <summary>
    /// Complete game state including board, score, active blocks, and game status.
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
        /// Active Game Mode for score rules and layout adjustments.
        /// </summary>
        public GameMode Mode { get; private set; }

        /// <summary>
        /// Number of times rescue/continue has been used in this run.
        /// </summary>
        public int RescueCount { get; private set; }
        
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
                            System.Diagnostics.Debug.WriteLine($"[GameState.AvailableShapes] CRITICAL: Slot {i} has ShapeId {shapeId} but ShapeLibrary.TryGetShape returned false!");
                            if (ShapeLibrary.TryGetShape(ShapeLibrary.Single, out var fallbackShape))
                            {
                                shapes[i] = fallbackShape;
                            }
                        }
                    }
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
        
        public GameState(int boardWidth = 10, int boardHeight = 10, GameMode mode = GameMode.Classic)
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
            Mode = mode;
            RescueCount = 0;
        }
        
        /// <summary>
        /// Creates a deep copy of this game state and all mutable child state.
        /// </summary>
        /// <returns>Deep copy of current state</returns>
        public GameState Clone()
        {
            if (Board == null)
                throw new InvalidOperationException("Cannot clone GameState when Board is null.");
            if (ActiveBlocks == null)
                throw new InvalidOperationException("Cannot clone GameState when ActiveBlocks is null.");
            if (ComboState == null)
                throw new InvalidOperationException("Cannot clone GameState when ComboState is null.");

            return new GameState(Board.Width, Board.Height, Mode)
            {
                Board = Board.Clone(),
                Score = Score,
                ActiveBlocks = ActiveBlocks.Clone(),
                ComboState = ComboState.Clone(),
                IsGameOver = IsGameOver,
                MoveCount = MoveCount,
                TotalLinesCleared = TotalLinesCleared,
                StartTime = StartTime,
                LastMoveTime = LastMoveTime,
                RescueCount = RescueCount
            };
        }

        /// <summary>
        /// Creates a deep snapshot of this state for isolation from later mutations.
        /// </summary>
        public GameState CreateSnapshot()
        {
            return Clone();
        }
        
        /// <summary>
        /// Creates a new game state with updated board.
        /// </summary>
        public GameState WithBoard(BoardState newBoard)
        {
            if (newBoard == null)
                throw new ArgumentNullException(nameof(newBoard));

            var newState = Clone();
            newState.Board = newBoard.Clone();
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state with updated score.
        /// </summary>
        public GameState WithScore(int newScore)
        {
            var newState = Clone();
            newState.Score = newScore < 0 ? 0 : newScore;
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state with updated active blocks.
        /// </summary>
        public GameState WithActiveBlocks(ActiveBlocks newActiveBlocks)
        {
            if (newActiveBlocks == null)
                throw new ArgumentNullException(nameof(newActiveBlocks));

            var newState = Clone();
            newState.ActiveBlocks = newActiveBlocks.Clone();
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state with updated combo state.
        /// </summary>
        public GameState WithComboState(ComboState newComboState)
        {
            if (newComboState == null)
                throw new ArgumentNullException(nameof(newComboState));

            var newState = Clone();
            newState.ComboState = newComboState.Clone();
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state marking game as over.
        /// </summary>
        public GameState WithGameOver()
        {
            var newState = Clone();
            newState.IsGameOver = true;
            return newState;
        }

        /// <summary>
        /// Creates a new game state with explicit game over flag value.
        /// </summary>
        public GameState WithGameOverState(bool isGameOver)
        {
            var newState = Clone();
            newState.IsGameOver = isGameOver;
            return newState;
        }
        
        /// <summary>
        /// Creates a new game state with incremented move count.
        /// </summary>
        public GameState WithIncrementedMoveCount()
        {
            var newState = Clone();
            if (newState.MoveCount < int.MaxValue)
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
        public GameState WithLinesCleared(int linesClearedThisMove)
        {
            if (linesClearedThisMove < 0)
                throw new ArgumentOutOfRangeException(nameof(linesClearedThisMove), "Lines cleared cannot be negative.");

            var newState = Clone();
            long nextTotal = (long)newState.TotalLinesCleared + linesClearedThisMove;
            newState.TotalLinesCleared = nextTotal > int.MaxValue ? int.MaxValue : (int)nextTotal;
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
        /// Creates a new game state with explicit rescue/continue count.
        /// </summary>
        public GameState WithRescueCount(int rescueCount)
        {
            var newState = Clone();
            newState.RescueCount = rescueCount < 0 ? 0 : rescueCount;
            return newState;
        }

        /// <summary>
        /// Creates a new game state with explicit game mode.
        /// </summary>
        public GameState WithGameMode(GameMode mode)
        {
            var newState = Clone();
            newState.Mode = mode;
            return newState;
        }
        
        /// <summary>
        /// Gets the elapsed game time.
        /// </summary>
        public TimeSpan GetElapsedTime()
        {
            return DateTime.Now - StartTime;
        }
        
        /// <summary>
        /// Gets the time since last move.
        /// </summary>
        public TimeSpan GetTimeSinceLastMove()
        {
            return DateTime.Now - LastMoveTime;
        }
        
        public override string ToString()
        {
            return $"Score: {Score}, Moves: {MoveCount}, Lines: {TotalLinesCleared}, " +
                   $"ActiveBlocks: {ActiveBlocks.Count}, GameOver: {IsGameOver}, Mode: {Mode}, Rescues: {RescueCount}";
        }
    }
}
