using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.UnityAdapter.Blocks;
using NUnit.Framework;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Tests
{
    public sealed class TrayLayoutCalculatorTests
    {
        [Test]
        public void GetShapeExtents_UsesOffsetsAndCellStep()
        {
            var shape = new ShapeDefinition(
                new ShapeId(99),
                "L",
                new[] { new Int2(0, 0), new Int2(1, 0), new Int2(0, 1) });

            var extents = TrayLayoutCalculator.GetShapeExtents(shape, 1.25f, 1f);

            Assert.That(extents.Left, Is.EqualTo(-0.5f).Within(0.0001f));
            Assert.That(extents.Right, Is.EqualTo(1.75f).Within(0.0001f));
            Assert.That(extents.Top, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(extents.Bottom, Is.EqualTo(-1.75f).Within(0.0001f));
            Assert.That(extents.Width, Is.EqualTo(2.25f).Within(0.0001f));
            Assert.That(extents.Height, Is.EqualTo(2.25f).Within(0.0001f));
        }

        [Test]
        public void Calculate_ResponsiveOverrideReturnsThreeSlotsAndBoundedScale()
        {
            var shapes = new[]
            {
                new ShapeDefinition(new ShapeId(1), "single", new[] { new Int2(0, 0) }),
                new ShapeDefinition(new ShapeId(2), "line", new[] { new Int2(0, 0), new Int2(1, 0) }),
                new ShapeDefinition(new ShapeId(3), "square", new[]
                {
                    new Int2(0, 0), new Int2(1, 0), new Int2(0, 1), new Int2(1, 1)
                })
            };
            var config = new TrayLayoutConfig(
                boardCellSize: 1f,
                boardCellSpacing: 0.1f,
                trayBlockScale: 1f,
                slotGap: 0.2f,
                trayHorizontalPadding: 0.5f,
                trayVerticalPadding: 0.25f,
                trayGapFromGrid: 0.1f,
                trayVerticalOffset: 0f,
                minTrayScale: 0.2f,
                hasResponsiveOverride: true,
                responsiveWidth: 10f,
                responsiveHeight: 3f,
                responsiveCenter: Vector3.zero);

            var result = TrayLayoutCalculator.Calculate(
                shapes, config, null, null, null, Vector3.zero, "test");

            Assert.That(result.UsedFallback, Is.False);
            Assert.That(result.SlotPositions, Has.Length.EqualTo(3));
            Assert.That(result.TrayScale, Is.InRange(0.2f, 1f));
            Assert.That(result.LayoutCamera, Is.Null);
            Assert.That(result.SlotPositions[0].x, Is.LessThan(result.SlotPositions[1].x));
            Assert.That(result.SlotPositions[1].x, Is.LessThan(result.SlotPositions[2].x));
        }
    }
}
