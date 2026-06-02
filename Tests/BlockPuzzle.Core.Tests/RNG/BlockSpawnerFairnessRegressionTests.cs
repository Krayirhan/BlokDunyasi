using System;
using System.Collections.Generic;
using System.Linq;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Shapes;
using NUnit.Framework;

namespace BlockPuzzle.Core.Tests.RNG
{
    [TestFixture]
    [Category("Regression")]
    [Category("Fairness")]
    public class BlockSpawnerFairnessRegressionTests
    {
        private static readonly int[] DeterministicSeedCorpus = Enumerable.Range(1001, 64).ToArray();

        [Test]
        public void SpawnBlockSet_SameSeedSameBoard_ProducesSameSet()
        {
            var board = CreateNarrowCorridorBoard();
            var firstSpawner = new BlockSpawner(new SeededRng(314159));
            var secondSpawner = new BlockSpawner(new SeededRng(314159));

            var first = firstSpawner.SpawnBlockSet(board);
            var second = secondSpawner.SpawnBlockSet(board);

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void SpawnBlockSet_SameSeedLongSequence_OnEmptyBoard_IsDeterministic()
        {
            var board = CreateEmptyBoard();
            var firstSpawner = new BlockSpawner(new SeededRng(271828));
            var secondSpawner = new BlockSpawner(new SeededRng(271828));

            for (int setIndex = 0; setIndex < 24; setIndex++)
            {
                var first = firstSpawner.SpawnBlockSet(board);
                var second = secondSpawner.SpawnBlockSet(board);
                CollectionAssert.AreEqual(first, second, $"Spawn set mismatch at sequence index {setIndex}.");
            }
        }

        [Test]
        public void SpawnBlockSet_DifferentSeeds_UsuallyDiverge()
        {
            var board = CreateEmptyBoard();
            var firstSpawner = new BlockSpawner(new SeededRng(11));
            var secondSpawner = new BlockSpawner(new SeededRng(12));

            bool foundDifference = false;
            for (int setIndex = 0; setIndex < 8; setIndex++)
            {
                var first = firstSpawner.SpawnBlockSet(board);
                var second = secondSpawner.SpawnBlockSet(board);
                if (!first.SequenceEqual(second))
                {
                    foundDifference = true;
                    break;
                }
            }

            Assert.IsTrue(foundDifference, "Different seeds should diverge across spawn sequence.");
        }

        [TestCaseSource(nameof(GetPlayableFixtureCases))]
        public void SpawnBlockSet_PlayableFixture_AlwaysContainsAtLeastOnePlaceableShape(string fixtureName, BoardState board)
        {
            var spawner = new BlockSpawner(new SeededRng(GetFixtureSeed(fixtureName)));

            var blockSet = spawner.SpawnBlockSet(board);
            int placeableCount = CountPlaceableShapes(blockSet, board);

            Assert.GreaterOrEqual(placeableCount, 1, $"Fixture '{fixtureName}' should always receive at least one placeable shape.");
        }

        [Test]
        public void TrueDeadBoard_HasNoPlacements_AndGameOverLogicAgrees()
        {
            var deadBoard = CreateTrueDeadBoard();

            foreach (var shape in ShapeLibrary.All)
            {
                Assert.IsFalse(
                    PlacementSearch.HasAnyValidPlacement(deadBoard, shape),
                    $"Dead board should not allow placement for shape {shape.Name}.");
            }

            var engine = new GameEngine(new SeededRng(5150), boardWidth: deadBoard.Width, boardHeight: deadBoard.Height);
            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            activeBlocks.SetBlockAt(1, new ShapeId(8));
            activeBlocks.SetBlockAt(2, new ShapeId(16));

            var state = new GameState(deadBoard.Width, deadBoard.Height)
                .WithBoard(deadBoard)
                .WithActiveBlocks(activeBlocks);

            engine.LoadGame(state);

            Assert.IsTrue(engine.IsGameOver(), "Game over detection should agree with dead-board fixture.");
        }

        [Test]
        public void HardBoardSeedCorpus_MeetsPlaceableThreshold()
        {
            var board = CreateFragmentedBoard();
            int placeableSets = 0;

            foreach (int seed in DeterministicSeedCorpus)
            {
                var spawner = new BlockSpawner(new SeededRng(seed));
                var blockSet = spawner.SpawnBlockSet(board);
                if (CountPlaceableShapes(blockSet, board) > 0)
                    placeableSets++;
            }

            float placeableRate = placeableSets / (float)DeterministicSeedCorpus.Length;
            Assert.GreaterOrEqual(placeableRate, 0.95f, $"Placeable rate was {placeableRate:P0}.");
        }

        [Test]
        public void NearlyFullOneCellHoleBoard_DoesNotProduceAllLargeImpossibleSets()
        {
            var board = CreateNearlyFullOneCellHolesBoard();

            foreach (int seed in DeterministicSeedCorpus)
            {
                var spawner = new BlockSpawner(new SeededRng(seed));
                var blockSet = spawner.SpawnBlockSet(board);
                bool hasPlaceableEasyShape = blockSet.Any(shapeId =>
                    ShapeLibrary.TryGetShape(shapeId, out var shape) &&
                    shape.Offsets.Length <= 2 &&
                    PlacementSearch.HasAnyValidPlacement(board, shape));

                Assert.IsTrue(
                    hasPlaceableEasyShape,
                    $"Seed {seed} produced no placeable easy shape on one-cell-hole fixture.");
            }
        }

        [Test]
        public void OneValidPlacementBoard_FixtureHasExactlyOnePlaceableShapeFamily()
        {
            var board = CreateOneValidPlacementBoard();
            var placeableShapes = GetPlaceableShapes(board);

            Assert.AreEqual(1, placeableShapes.Count, "Fixture should only allow a single placeable shape family.");
            Assert.AreEqual(ShapeLibrary.Single, placeableShapes[0].Id);
        }

        [Test]
        public void BoardHeuristics_EmptyBoardScoresHigherThanFragmentedBoard()
        {
            var empty = CreateEmptyBoard();
            var fragmented = CreateFragmentedBoard();

            var emptySnapshot = BoardHeuristics.Evaluate(empty, ShapeLibrary.All);
            var fragmentedSnapshot = BoardHeuristics.Evaluate(fragmented, ShapeLibrary.All);

            Assert.Greater(emptySnapshot.GetCompositeScore(), fragmentedSnapshot.GetCompositeScore());
            Assert.Greater(emptySnapshot.AvailableThreeByThreeCount, fragmentedSnapshot.AvailableThreeByThreeCount);
            Assert.Greater(emptySnapshot.LargestConnectedEmptyRegion, fragmentedSnapshot.LargestConnectedEmptyRegion);
            Assert.Greater(emptySnapshot.PlaceableShapeCount, fragmentedSnapshot.PlaceableShapeCount);
        }

        private static IEnumerable<TestCaseData> GetPlayableFixtureCases()
        {
            yield return new TestCaseData("Empty board", CreateEmptyBoard());
            yield return new TestCaseData("Nearly full", CreateNearlyFullOneCellHolesBoard());
            yield return new TestCaseData("Narrow corridor", CreateNarrowCorridorBoard());
            yield return new TestCaseData("Fragmented", CreateFragmentedBoard());
            yield return new TestCaseData("One valid placement", CreateOneValidPlacementBoard());
        }

        private static int GetFixtureSeed(string fixtureName)
        {
            return fixtureName switch
            {
                "Empty board" => 101,
                "Nearly full" => 202,
                "Narrow corridor" => 303,
                "Fragmented" => 404,
                "One valid placement" => 505,
                _ => 777
            };
        }

        private static BoardState CreateEmptyBoard()
        {
            return new BoardState(8, 8);
        }

        private static BoardState CreateNearlyFullOneCellHolesBoard()
        {
            return CreateFilledBoardWithEmptyCells(6, 6,
                new Int2(0, 0),
                new Int2(2, 2),
                new Int2(5, 5));
        }

        private static BoardState CreateNarrowCorridorBoard()
        {
            return CreateFilledBoardWithPredicate(6, 6, (x, _) => x == 2);
        }

        private static BoardState CreateFragmentedBoard()
        {
            return CreateFilledBoardWithPredicate(6, 6, (x, y) => ((x + y) % 2) == 0);
        }

        private static BoardState CreateOneValidPlacementBoard()
        {
            return CreateFilledBoardWithEmptyCells(4, 4, new Int2(3, 3));
        }

        private static BoardState CreateTrueDeadBoard()
        {
            return CreateFilledBoardWithEmptyCells(4, 4);
        }

        private static BoardState CreateFilledBoardWithEmptyCells(int width, int height, params Int2[] emptyCells)
        {
            var emptyLookup = new HashSet<Int2>(emptyCells ?? Array.Empty<Int2>());
            return CreateFilledBoardWithPredicate(width, height, (x, y) => emptyLookup.Contains(new Int2(x, y)));
        }

        private static BoardState CreateFilledBoardWithPredicate(int width, int height, Func<int, int, bool> shouldBeEmpty)
        {
            var board = new BoardState(width, height);
            var cells = new CellState[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    cells[index] = shouldBeEmpty(x, y)
                        ? CellState.Empty
                        : CellState.Filled(99, 1);
                }
            }

            board.SetCells(cells);
            return board;
        }

        private static int CountPlaceableShapes(ShapeId[] blockSet, BoardState board)
        {
            int count = 0;
            foreach (var shapeId in blockSet)
            {
                if (ShapeLibrary.TryGetShape(shapeId, out var shape) &&
                    PlacementSearch.HasAnyValidPlacement(board, shape))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<ShapeDefinition> GetPlaceableShapes(BoardState board)
        {
            var result = new List<ShapeDefinition>();
            foreach (var shape in ShapeLibrary.All)
            {
                if (PlacementSearch.HasAnyValidPlacement(board, shape))
                    result.Add(shape);
            }

            return result;
        }
    }
}
