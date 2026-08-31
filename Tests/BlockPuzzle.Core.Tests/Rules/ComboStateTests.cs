using NUnit.Framework;
using BlockPuzzle.Core.Rules;

namespace BlockPuzzle.Core.Tests.Rules
{
    [TestFixture]
    [Category("Unit")]
    public class ComboStateTests
    {
        [Test]
        public void UpdateCombo_WithNoLines_ConsumesGraceBeforeResetting()
        {
            var combo = new ComboState();
            combo.IncrementCombo();
            combo.IncrementCombo(); // streak = 2, GraceUsed = false

            combo.UpdateCombo(0);

            Assert.AreEqual(2, combo.Streak);
            Assert.IsTrue(combo.GraceUsed);
            Assert.AreEqual(0, combo.GraceMovesRemaining);
        }

        [Test]
        public void UpdateCombo_WithClears_IncrementsCombo()
        {
            var combo = new ComboState();

            combo.UpdateCombo(1);
            combo.UpdateCombo(2);

            Assert.AreEqual(2, combo.Streak);
            Assert.IsFalse(combo.GraceUsed);
            Assert.AreEqual(1, combo.GraceMovesRemaining);
        }

        [Test]
        public void ConsumeNonClearMove_AfterGraceIsSpent_ResetsCombo()
        {
            var combo = new ComboState();
            combo.IncrementCombo();
            combo.IncrementCombo(); // streak = 2, GraceUsed = false

            combo.ConsumeNonClearMove(); // GraceUsed becomes true, streak = 2
            Assert.AreEqual(2, combo.Streak);
            Assert.IsTrue(combo.GraceUsed);

            combo.ConsumeNonClearMove(); // Reset streak to 0, GraceUsed to false

            Assert.AreEqual(0, combo.Streak);
            Assert.IsFalse(combo.GraceUsed);
            Assert.AreEqual(0, combo.GraceMovesRemaining);
        }

        [Test]
        public void ResetCombo_ReturnsToDefaultValues()
        {
            var combo = new ComboState();
            combo.IncrementCombo();
            combo.IncrementCombo();

            combo.ResetCombo();

            Assert.AreEqual(0, combo.Streak);
            Assert.IsFalse(combo.GraceUsed);
            Assert.AreEqual(0, combo.GraceMovesRemaining);
        }

        [Test]
        public void SetStreak_ResetsGraceUsed()
        {
            var combo = new ComboState();
            combo.SetStreak(-5);
            Assert.AreEqual(0, combo.Streak);

            combo.SetStreak(5);
            Assert.AreEqual(5, combo.Streak);
            Assert.IsFalse(combo.GraceUsed);
        }
    }
}
