using NUnit.Framework;
using BlockPuzzle.Core.Rules;

namespace BlockPuzzle.Core.Tests.Rules
{
    [TestFixture]
    [Category("Unit")]
    public class ComboStateTests
    {
        [Test]
        public void UpdateCombo_WithNoLines_ResetsCombo()
        {
            var combo = new ComboState();
            combo.IncrementCombo();
            combo.IncrementCombo();

            combo.UpdateCombo(0);

            Assert.AreEqual(0, combo.Streak);
            Assert.AreEqual(1.0f, combo.Multiplier);
        }

        [Test]
        public void UpdateCombo_WithClears_IncrementsCombo()
        {
            var combo = new ComboState();

            combo.UpdateCombo(1);
            combo.UpdateCombo(2);

            Assert.AreEqual(2, combo.Streak);
            Assert.AreEqual(1.1f, combo.Multiplier);
        }

        [Test]
        public void ResetCombo_ReturnsToDefaultValues()
        {
            var combo = new ComboState();
            combo.IncrementCombo();
            combo.IncrementCombo();

            combo.ResetCombo();

            Assert.AreEqual(0, combo.Streak);
            Assert.AreEqual(1.0f, combo.Multiplier);
        }

        [Test]
        public void SetStreak_WithNegativeValue_ClampsToZero()
        {
            var combo = new ComboState();

            combo.SetStreak(-5);

            Assert.AreEqual(0, combo.Streak);
            Assert.AreEqual(1.0f, combo.Multiplier);
        }
    }
}
