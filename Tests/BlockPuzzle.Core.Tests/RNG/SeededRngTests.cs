using NUnit.Framework;
using BlockPuzzle.Core.RNG;

namespace BlockPuzzle.Core.Tests.RNG
{
    [TestFixture]
    [Category("Unit")]
    public class SeededRngTests
    {
        [Test]
        public void Reseed_SameSeed_ReproducesSameSequence()
        {
            var rng = new SeededRng(12345);

            var firstSequence = new[]
            {
                rng.Next(1000),
                rng.Next(1000),
                rng.Next(1000),
                rng.Next(1000),
                rng.Next(1000)
            };

            rng.Reseed(12345);

            var secondSequence = new[]
            {
                rng.Next(1000),
                rng.Next(1000),
                rng.Next(1000),
                rng.Next(1000),
                rng.Next(1000)
            };

            CollectionAssert.AreEqual(firstSequence, secondSequence);
        }
    }
}
