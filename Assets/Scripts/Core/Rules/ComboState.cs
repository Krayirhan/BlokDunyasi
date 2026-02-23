// File: Core/Rules/ComboState.cs
namespace BlockPuzzle.Core.Rules
{
    /// <summary>
    /// Tracks the current combo state for scoring multipliers.
    /// 
    /// Rules:
    /// - Combo resets when no lines are cleared in a move
    /// - Combo increases when lines are cleared
    /// - Multiplier is based on combo streak
    /// </summary>
    public class ComboState
    {
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
                // No lines cleared - reset combo
                Reset();
            }
            else
            {
                // Lines cleared - increment combo
                Streak++;
                Multiplier = CalculateMultiplier(Streak);
            }
        }
        
        /// <summary>
        /// Increments combo and returns new state.
        /// </summary>
        /// <returns>This ComboState after incrementing</returns>
        public ComboState IncrementCombo()
        {
            Streak++;
            Multiplier = CalculateMultiplier(Streak);
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
        }

        /// <summary>
        /// Restores combo streak directly (for persistence).
        /// </summary>
        public void SetStreak(int streak)
        {
            Streak = streak < 0 ? 0 : streak;
            Multiplier = CalculateMultiplier(Streak);
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
                Multiplier = this.Multiplier
            };
        }
        
        /// <summary>
        /// Calculates the multiplier for a given combo streak.
        /// Formula: 1.0 + (streak - 1) * 0.1 (10% increase per combo level)
        /// </summary>
        /// <param name="streak">Combo streak level</param>
        /// <returns>Score multiplier</returns>
        private static float CalculateMultiplier(int streak)
        {
            if (streak <= 0) return 1.0f;
            return 1.0f + (streak - 1) * 0.1f;
        }
        
        public override string ToString()
        {
            return $"Combo: {Streak} (x{Multiplier:F1})";
        }
    }
}
