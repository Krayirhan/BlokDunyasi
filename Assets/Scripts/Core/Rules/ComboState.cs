// File: Core/Rules/ComboState.cs
namespace BlockPuzzle.Core.Rules
{
    /// <summary>
    /// Tracks the current combo state for scoring multipliers.
    /// 
    /// Rules:
    /// - Combo increases when lines are cleared
    /// - One setup move without a clear can be buffered before the combo breaks
    /// - A second consecutive non-clear move resets the combo
    /// - Multiplier is based on combo streak
    /// </summary>
    public class ComboState
    {
        private const int DefaultGraceMovesAfterClear = 1;
        private const int MaxComboStreak = 50;

        /// <summary>
        /// Current combo streak (number of consecutive moves with line clears).
        /// </summary>
        public int Streak { get; private set; }
        
        /// <summary>
        /// Alias for Streak for backward compatibility.
        /// </summary>
        public int CurrentStreak => Streak;
        
        /// <summary>
        /// Current combo multiplier based on streak.
        /// </summary>
        public float Multiplier { get; private set; }

        /// <summary>
        /// Remaining non-clear setup moves that can be made before the combo breaks.
        /// </summary>
        public int GraceMovesRemaining { get; private set; }
        
        /// <summary>
        /// Creates a new combo state with no active combo.
        /// </summary>
        public ComboState()
        {
            Reset();
        }
        
        /// <summary>
        /// Updates combo state based on lines cleared this move.
        /// </summary>
        /// <param name="linesClearedThisMove">Number of lines cleared</param>
        public void UpdateCombo(int linesClearedThisMove)
        {
            if (linesClearedThisMove == 0)
            {
                ConsumeNonClearMove();
            }
            else
            {
                IncrementCombo();
            }
        }
        
        /// <summary>
        /// Increments combo and returns new state.
        /// </summary>
        /// <returns>This ComboState after incrementing</returns>
        public ComboState IncrementCombo()
        {
            Streak = System.Math.Min(MaxComboStreak, Streak + 1);
            Multiplier = CalculateMultiplier(Streak);
            GraceMovesRemaining = DefaultGraceMovesAfterClear;
            return this;
        }

        /// <summary>
        /// Applies a non-clear move to the combo chain.
        /// Allows one buffered setup move before fully resetting the streak.
        /// </summary>
        public ComboState ConsumeNonClearMove()
        {
            if (Streak <= 0)
            {
                Reset();
                return this;
            }

            if (GraceMovesRemaining > 0)
            {
                GraceMovesRemaining--;
                return this;
            }

            Reset();
            return this;
        }
        
        /// <summary>
        /// Resets combo and returns new state.
        /// </summary>
        /// <returns>This ComboState after resetting</returns>
        public ComboState ResetCombo()
        {
            Reset();
            return this;
        }
        
        /// <summary>
        /// Resets the combo state.
        /// </summary>
        public void Reset()
        {
            Streak = 0;
            Multiplier = 1.0f;
            GraceMovesRemaining = 0;
        }

        /// <summary>
        /// Restores combo streak directly (for persistence).
        /// </summary>
        public void SetStreak(int streak)
        {
            Streak = streak < 0 ? 0 : streak;
            Multiplier = CalculateMultiplier(Streak);
            GraceMovesRemaining = Streak > 0 ? DefaultGraceMovesAfterClear : 0;
        }

        /// <summary>
        /// Restores both streak and buffered grace moves directly (for persistence).
        /// </summary>
        public void SetState(int streak, int graceMovesRemaining)
        {
            Streak = streak < 0 ? 0 : streak;
            Multiplier = CalculateMultiplier(Streak);
            GraceMovesRemaining = Streak > 0 ? System.Math.Max(0, graceMovesRemaining) : 0;
        }
        
        /// <summary>
        /// Creates a copy of this combo state.
        /// </summary>
        /// <returns>New ComboState with same values</returns>
        public ComboState Clone()
        {
            return new ComboState
            {
                Streak = this.Streak,
                Multiplier = this.Multiplier,
                GraceMovesRemaining = this.GraceMovesRemaining
            };
        }
        
        /// <summary>
        /// Calculates the multiplier for a given combo streak.
        /// Multiplier source is the active <see cref="ScoringRules.DefaultConfig"/> combo curve.
        /// </summary>
        /// <param name="streak">Combo streak level</param>
        /// <returns>Score multiplier</returns>
        private static float CalculateMultiplier(int streak)
        {
            if (streak <= 0)
                return 1.0f;

            return ScoringRules.DefaultConfig.EvaluateComboMultiplier(streak);
        }
        
        public override string ToString()
        {
            return $"Combo: {Streak} (x{Multiplier:F1}, grace {GraceMovesRemaining})";
        }
    }
}
