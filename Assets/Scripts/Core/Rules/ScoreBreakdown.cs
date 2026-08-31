namespace BlockPuzzle.Core.Rules
{
    /// <summary>
    /// Struct representing the detailed score breakdown for a single move.
    /// </summary>
    public struct ScoreBreakdown
    {
        public int PlacementScore;
        public int LineClearScore;
        public int ComboBonus;
        public int RiskBonus;
        public int TotalGained;
        public int LinesCleared;
        public int ComboCount;
        public bool UsedGrace;
        public bool ComboBroken;
        public bool IsEdgeBonus;
        public bool IsCornerBonus;
    }
}
