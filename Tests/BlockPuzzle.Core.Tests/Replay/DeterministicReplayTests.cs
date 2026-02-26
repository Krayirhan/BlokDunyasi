using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.RNG;
using NUnit.Framework;

namespace BlockPuzzle.Core.Tests.Replay
{
    [TestFixture]
    [Category("Replay")]
    [Category("Integration")]
    public class DeterministicReplayTests
    {
        [Test]
        public void SameSeedAndSameMoves_ProducesSameScoreAndBoard()
        {
            const int seed = 777123;
            const int maxMoves = 30;

            var firstRun = CreateEngine(seed);
            var replayMoves = CaptureReplayMoves(firstRun, maxMoves);
            Assert.GreaterOrEqual(replayMoves.Count, 8, "Replay test needs enough moves to be meaningful.");
            var expectedSnapshot = Snapshot.FromState(firstRun.CurrentState);

            var secondRun = CreateEngine(seed);
            ReplayAndAssert(secondRun, replayMoves);
            var actualSnapshot = Snapshot.FromState(secondRun.CurrentState);

            Assert.AreEqual(expectedSnapshot.Score, actualSnapshot.Score);
            Assert.AreEqual(expectedSnapshot.Combo, actualSnapshot.Combo);
            Assert.AreEqual(expectedSnapshot.TotalLinesCleared, actualSnapshot.TotalLinesCleared);
            Assert.AreEqual(expectedSnapshot.MoveCount, actualSnapshot.MoveCount);
            CollectionAssert.AreEqual(expectedSnapshot.BoardCells, actualSnapshot.BoardCells);
            CollectionAssert.AreEqual(expectedSnapshot.ActiveBlockSlots, actualSnapshot.ActiveBlockSlots);
            CollectionAssert.AreEqual(expectedSnapshot.ActiveBlockColors, actualSnapshot.ActiveBlockColors);
        }

        private static GameEngine CreateEngine(int seed)
        {
            var engine = new GameEngine(new SeededRng(seed), boardWidth: 10, boardHeight: 10);
            engine.StartNewGame(seed);
            return engine;
        }

        private static List<ReplayMove> CaptureReplayMoves(GameEngine engine, int maxMoves)
        {
            var result = new List<ReplayMove>(maxMoves);

            for (int moveIndex = 0; moveIndex < maxMoves; moveIndex++)
            {
                if (engine.IsGameOver())
                    break;

                if (!TryFindDeterministicMove(engine, out int slotIndex, out Int2 position))
                    break;

                var move = engine.AttemptMove(slotIndex, position);
                Assert.IsTrue(move.Success, $"Move {moveIndex} should be valid during capture.");

                result.Add(new ReplayMove(
                    slotIndex,
                    position,
                    move.ScoreDelta,
                    move.TotalScore,
                    engine.CurrentState.Combo,
                    engine.CurrentState.TotalLinesCleared));
            }

            return result;
        }

        private static void ReplayAndAssert(GameEngine engine, IReadOnlyList<ReplayMove> replayMoves)
        {
            for (int i = 0; i < replayMoves.Count; i++)
            {
                var expected = replayMoves[i];

                Assert.IsTrue(
                    engine.IsValidMove(expected.SlotIndex, expected.Position),
                    $"Replay move {i} became invalid. Slot={expected.SlotIndex}, Pos={expected.Position}");

                var move = engine.AttemptMove(expected.SlotIndex, expected.Position);
                Assert.IsTrue(move.Success, $"Replay move {i} failed.");
                Assert.AreEqual(expected.ScoreDelta, move.ScoreDelta, $"Score delta mismatch at replay move {i}.");
                Assert.AreEqual(expected.TotalScore, move.TotalScore, $"Total score mismatch at replay move {i}.");
                Assert.AreEqual(expected.ComboAfterMove, engine.CurrentState.Combo, $"Combo mismatch at replay move {i}.");
                Assert.AreEqual(expected.TotalLinesClearedAfterMove, engine.CurrentState.TotalLinesCleared, $"Line total mismatch at replay move {i}.");
            }
        }

        private static bool TryFindDeterministicMove(GameEngine engine, out int slotIndex, out Int2 position)
        {
            for (int slot = 0; slot < 3; slot++)
            {
                if (!engine.CurrentState.ActiveBlocks.HasBlockAt(slot))
                    continue;

                var placements = engine.GetValidPlacements(slot);
                if (placements == null || placements.Length == 0)
                    continue;

                Array.Sort(placements, ComparePositions);
                slotIndex = slot;
                position = placements[0];
                return true;
            }

            slotIndex = -1;
            position = Int2.Zero;
            return false;
        }

        private static int ComparePositions(Int2 a, Int2 b)
        {
            int yCompare = a.Y.CompareTo(b.Y);
            return yCompare != 0 ? yCompare : a.X.CompareTo(b.X);
        }

        private readonly struct ReplayMove
        {
            public readonly int SlotIndex;
            public readonly Int2 Position;
            public readonly int ScoreDelta;
            public readonly int TotalScore;
            public readonly int ComboAfterMove;
            public readonly int TotalLinesClearedAfterMove;

            public ReplayMove(int slotIndex, Int2 position, int scoreDelta, int totalScore, int comboAfterMove, int totalLinesClearedAfterMove)
            {
                SlotIndex = slotIndex;
                Position = position;
                ScoreDelta = scoreDelta;
                TotalScore = totalScore;
                ComboAfterMove = comboAfterMove;
                TotalLinesClearedAfterMove = totalLinesClearedAfterMove;
            }
        }

        private readonly struct Snapshot
        {
            public readonly int Score;
            public readonly int Combo;
            public readonly int TotalLinesCleared;
            public readonly int MoveCount;
            public readonly CellState[] BoardCells;
            public readonly int[] ActiveBlockSlots;
            public readonly int[] ActiveBlockColors;

            public Snapshot(int score, int combo, int totalLinesCleared, int moveCount, CellState[] boardCells, int[] activeBlockSlots, int[] activeBlockColors)
            {
                Score = score;
                Combo = combo;
                TotalLinesCleared = totalLinesCleared;
                MoveCount = moveCount;
                BoardCells = boardCells;
                ActiveBlockSlots = activeBlockSlots;
                ActiveBlockColors = activeBlockColors;
            }

            public static Snapshot FromState(GameState state)
            {
                var colors = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    if (state.ActiveBlocks.TryGetColorId(i, out int color))
                        colors[i] = color;
                }

                return new Snapshot(
                    state.Score,
                    state.Combo,
                    state.TotalLinesCleared,
                    state.MoveCount,
                    state.Board.GetCells(),
                    state.ActiveBlocks.GetSlotIds(),
                    colors);
            }
        }
    }
}
