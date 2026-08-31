// File: Core/Rules/ComboState.cs
namespace BlockPuzzle.Core.Rules
{
    /// <summary>
    /// Tracks the current combo state for scoring multipliers (Version 3.0).
    /// </summary>
    public class ComboState
    {
        private const int MaxComboStreak = 50;

        /// <summary>
        /// Current combo streak.
        /// </summary>
        public int Streak { get; private set; }
        
        public int CurrentStreak => Streak;
        
        /// <summary>
        /// Combo multiplier (not directly used by 3.0, but kept for compatibility).
        /// </summary>
        public float Multiplier { get; private set; }

        /// <summary>
        /// Whether the combo grace (one setup move) has been spent.
        /// </summary>
        public bool GraceUsed { get; set; }

        /// <summary>
        /// Remaining grace moves (for backwards compatibility).
        /// </summary>
        public int GraceMovesRemaining
        {
            get => (Streak > 0 && !GraceUsed) ? 1 : 0;
            set => GraceUsed = (value == 0);
        }
        
        public ComboState()
        {
            Reset();
        }
        
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
        
        public ComboState IncrementCombo()
        {
            Streak = System.Math.Min(MaxComboStreak, Streak + 1);
            Multiplier = CalculateMultiplier(Streak);
            GraceUsed = false;
            return this;
        }

        public ComboState ConsumeNonClearMove()
        {
            if (Streak <= 0)
            {
                Reset();
                return this;
            }

            if (!GraceUsed)
            {
                GraceUsed = true;
                return this;
            }

            Reset();
            return this;
        }
        
        public ComboState ResetCombo()
        {
            Reset();
            return this;
        }
        
        public void Reset()
        {
            Streak = 0;
            Multiplier = 1.0f;
            GraceUsed = false;
        }

        public void SetStreak(int streak)
        {
            Streak = streak < 0 ? 0 : streak;
            Multiplier = CalculateMultiplier(Streak);
            GraceUsed = false;
        }

        public void SetState(int streak, int graceMovesRemaining)
        {
            Streak = streak < 0 ? 0 : streak;
            Multiplier = CalculateMultiplier(Streak);
            GraceUsed = (graceMovesRemaining == 0);
        }
        
        public ComboState Clone()
        {
            return new ComboState
            {
                Streak = this.Streak,
                Multiplier = this.Multiplier,
                GraceUsed = this.GraceUsed
            };
        }
        
        private static float CalculateMultiplier(int streak)
        {
            if (streak <= 0)
                return 1.0f;

            return ScoringRules.DefaultConfig.EvaluateComboMultiplier(streak);
        }
        
        public override string ToString()
        {
            return $"Combo: {Streak} (x{Multiplier:F1}, graceUsed {GraceUsed})";
        }
    }
}
