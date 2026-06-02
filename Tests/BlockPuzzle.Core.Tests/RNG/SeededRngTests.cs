using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.RNG;
using NUnit.Framework;

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

        [Test]
        public void Clone_AtInitialState_ProducesSameFutureSequence()
        {
            var rng = new SeededRng(123);
            var clone = rng.Clone();

            for (int i = 0; i < 32; i++)
            {
                Assert.AreEqual(rng.Next(), clone.Next(), $"Mismatch at step {i}.");
            }
        }

        [Test]
        public void Clone_AfterAdvances_ProducesSameContinuation()
        {
            var rng = new SeededRng(123);
            rng.Next();
            rng.Next();
            rng.Next(10, 100);
            rng.NextFloat();

            var clone = rng.Clone();

            for (int i = 0; i < 32; i++)
            {
                Assert.AreEqual(rng.Next(), clone.Next(), $"Mismatch at continuation step {i}.");
            }
        }

        [Test]
        public void Clone_IsIndependentFromOriginalState()
        {
            var rng = new SeededRng(98765);
            rng.Next();
            var clone = rng.Clone();

            int cloneFirst = clone.Next();
            int originalFirst = rng.Next();

            Assert.AreEqual(originalFirst, cloneFirst);

            int originalSecond = rng.Next();
            int cloneSecond = clone.Next();

            Assert.AreEqual(originalSecond, cloneSecond);
        }

        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var first = new SeededRng(20260520);
            var second = new SeededRng(20260520);

            for (int i = 0; i < 64; i++)
            {
                Assert.AreEqual(first.Next(1000000), second.Next(1000000), $"Mismatch at step {i}.");
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequence()
        {
            var first = new SeededRng(1);
            var second = new SeededRng(2);

            bool foundDifference = false;
            for (int i = 0; i < 16; i++)
            {
                if (first.Next() != second.Next())
                {
                    foundDifference = true;
                    break;
                }
            }

            Assert.IsTrue(foundDifference, "Different seeds should diverge quickly.");
        }

        [Test]
        public void Range_StaysWithinBounds()
        {
            var rng = new SeededRng(999);

            for (int i = 0; i < 500; i++)
            {
                int value = rng.Next(-7, 13);
                Assert.GreaterOrEqual(value, -7);
                Assert.Less(value, 13);
            }
        }

        [Test]
        public void ReplayBranching_ClonePreservesBranchStart()
        {
            var rng = new SeededRng(456);

            for (int i = 0; i < 11; i++)
                rng.Next();

            var branch = rng.Clone();

            for (int i = 0; i < 24; i++)
            {
                Assert.AreEqual(rng.Next(2048), branch.Next(2048), $"Branch mismatch at step {i}.");
            }
        }

        [Test]
        public void BlockSpawner_SameSeed_ProducesSameBlockSequence()
        {
            var board = new BoardState(10, 10);
            var firstSpawner = new BlockSpawner(new SeededRng(24680));
            var secondSpawner = new BlockSpawner(new SeededRng(24680));

            for (int setIndex = 0; setIndex < 8; setIndex++)
            {
                var first = firstSpawner.SpawnBlockSet(board);
                var second = secondSpawner.SpawnBlockSet(board);
                CollectionAssert.AreEqual(first, second, $"Spawn set mismatch at index {setIndex}.");
            }
        }
    }
}
