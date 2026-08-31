using NUnit.Framework;
using System.Collections.Generic;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Board;

namespace BlockPuzzle.Core.Tests.Rules
{
    [TestFixture]
    [Category("Unit")]
    public class ScoringRulesTests
    {
        [Test]
        public void Test1_Placement_NoClear_Classic()
        {
            int comboCount = 0;
            bool graceUsed = false;
            var breakdown = ScoringRules.CalculateMoveScore(null, 4, 0, null, GameMode.Classic, ref comboCount, ref graceUsed);

            Assert.AreEqual(100, breakdown.PlacementScore);
            Assert.AreEqual(0, breakdown.LineClearScore);
            Assert.AreEqual(0, breakdown.ComboBonus);
            Assert.AreEqual(100, breakdown.TotalGained);
            Assert.AreEqual(0, comboCount);
            Assert.IsFalse(graceUsed);
            Assert.IsFalse(breakdown.UsedGrace);
            Assert.IsFalse(breakdown.ComboBroken);
        }

        [Test]
        public void Test2_TwoLineClear_ComboStart0()
        {
            int comboCount = 0;
            bool graceUsed = false;
            var breakdown = ScoringRules.CalculateMoveScore(null, 0, 2, null, GameMode.Classic, ref comboCount, ref graceUsed);

            Assert.AreEqual(0, breakdown.PlacementScore);
            Assert.AreEqual(800, breakdown.LineClearScore);
            Assert.AreEqual(200, breakdown.ComboBonus); // 1 * 100 * 2
            Assert.AreEqual(1000, breakdown.TotalGained);
            Assert.AreEqual(1, comboCount);
            Assert.IsFalse(graceUsed);
        }

        [Test]
        public void Test3_OneLineClear_ComboStart1()
        {
            int comboCount = 1;
            bool graceUsed = false;
            var breakdown = ScoringRules.CalculateMoveScore(null, 0, 1, null, GameMode.Classic, ref comboCount, ref graceUsed);

            Assert.AreEqual(0, breakdown.PlacementScore);
            Assert.AreEqual(300, breakdown.LineClearScore);
            Assert.AreEqual(200, breakdown.ComboBonus); // 2 * 100 * 1
            Assert.AreEqual(500, breakdown.TotalGained);
            Assert.AreEqual(2, comboCount);
            Assert.IsFalse(graceUsed);
        }

        [Test]
        public void Test4_NoClear_ComboStart2_GraceUsedFalse()
        {
            int comboCount = 2;
            bool graceUsed = false;
            var breakdown = ScoringRules.CalculateMoveScore(null, 4, 0, null, GameMode.Classic, ref comboCount, ref graceUsed);

            Assert.AreEqual(100, breakdown.PlacementScore);
            Assert.AreEqual(0, breakdown.LineClearScore);
            Assert.AreEqual(0, breakdown.ComboBonus);
            Assert.AreEqual(100, breakdown.TotalGained);
            Assert.AreEqual(2, comboCount);
            Assert.IsTrue(graceUsed);
            Assert.IsTrue(breakdown.UsedGrace);
            Assert.IsFalse(breakdown.ComboBroken);
        }

        [Test]
        public void Test5_NoClear_ComboStart2_GraceUsedTrue()
        {
            int comboCount = 2;
            bool graceUsed = true;
            var breakdown = ScoringRules.CalculateMoveScore(null, 4, 0, null, GameMode.Classic, ref comboCount, ref graceUsed);

            Assert.AreEqual(100, breakdown.PlacementScore);
            Assert.AreEqual(0, breakdown.LineClearScore);
            Assert.AreEqual(0, breakdown.ComboBonus);
            Assert.AreEqual(100, breakdown.TotalGained);
            Assert.AreEqual(0, comboCount);
            Assert.IsFalse(graceUsed);
            Assert.IsFalse(breakdown.UsedGrace);
            Assert.IsTrue(breakdown.ComboBroken);
        }

        [Test]
        public void Test6_FourLineClear_ComboStart25()
        {
            int comboCount = 25;
            bool graceUsed = false;
            var breakdown = ScoringRules.CalculateMoveScore(null, 0, 4, null, GameMode.Classic, ref comboCount, ref graceUsed);

            Assert.AreEqual(0, breakdown.PlacementScore);
            Assert.AreEqual(2200, breakdown.LineClearScore);
            Assert.AreEqual(10000, breakdown.ComboBonus); // Min(26, 25) * 100 * 4 = 10000
            Assert.AreEqual(12200, breakdown.TotalGained);
            Assert.AreEqual(26, comboCount);
        }

        [Test]
        public void Test7_FourLineClear_ComboStart30()
        {
            int comboCount = 30;
            bool graceUsed = false;
            var breakdown = ScoringRules.CalculateMoveScore(null, 0, 4, null, GameMode.Classic, ref comboCount, ref graceUsed);

            Assert.AreEqual(0, breakdown.PlacementScore);
            Assert.AreEqual(2200, breakdown.LineClearScore);
            Assert.AreEqual(10000, breakdown.ComboBonus); // Min(31, 25) * 100 * 4 = 10000
            Assert.AreEqual(12200, breakdown.TotalGained);
            Assert.AreEqual(31, comboCount);
        }

        [Test]
        public void Test10_RiskBonus_EdgeAndCorner()
        {
            var board = new BoardState(10, 10);
            
            var cornerPositionsClean = new List<Int2>
            {
                new Int2(0, 3),
                new Int2(5, 0)
            };

            int bonusCorner = ScoringRules.CalculateRiskBonus(board, cornerPositionsClean, out bool isEdge, out bool isCorner);
            Assert.IsTrue(isCorner);
            Assert.IsFalse(isEdge);
            Assert.AreEqual(40, bonusCorner);

            // Edge Case: Touches edge at 2 cells
            var edgePositions = new List<Int2>
            {
                new Int2(0, 3), // left edge
                new Int2(0, 4)  // left edge
            };

            int bonusEdge = ScoringRules.CalculateRiskBonus(board, edgePositions, out isEdge, out isCorner);
            Assert.IsFalse(isCorner);
            Assert.IsTrue(isEdge);
            Assert.AreEqual(20, bonusEdge);
        }
    }
}
