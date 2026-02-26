using NUnit.Framework;
using BlockPuzzle.Core.Engine;

namespace BlockPuzzle.Core.Tests.Engine
{
    [TestFixture]
    [Category("Unit")]
    public class GameStateSafetyTests
    {
        [Test]
        public void WithLinesCleared_NegativeValue_Throws()
        {
            var state = new GameState(4, 4);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => state.WithLinesCleared(-1));
        }

        [Test]
        public void WithLinesCleared_SaturatesAtIntMax()
        {
            var state = new GameState(4, 4).WithTotalLinesCleared(int.MaxValue - 1);

            var updated = state.WithLinesCleared(10);

            Assert.AreEqual(int.MaxValue, updated.TotalLinesCleared);
        }

        [Test]
        public void WithIncrementedMoveCount_WhenAtIntMax_DoesNotOverflow()
        {
            var state = new GameState(4, 4).WithMoveCount(int.MaxValue);

            var updated = state.WithIncrementedMoveCount();

            Assert.AreEqual(int.MaxValue, updated.MoveCount);
        }
    }
}
