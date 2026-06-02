#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using BlockPuzzle.UnityAdapter.Grid;

namespace BlockPuzzle.Tests.UnityAdapter
{
    public class CoordinateMapperTests
    {
        [Test]
        public void CoordinateMapper_GridToWorld_MapsTopLeftCorrectly()
        {
            var worldPos = CoordinateMapper.GridToWorldPosition(0, 0, 8, 8, 1f, Vector3.zero);
            Assert.AreEqual(-3.5f, worldPos.x);
            Assert.AreEqual(3.5f, worldPos.y);
        }

        [Test]
        public void CoordinateMapper_GridToWorld_MapsBottomRightCorrectly()
        {
            var worldPos = CoordinateMapper.GridToWorldPosition(7, 7, 8, 8, 1f, Vector3.zero);
            Assert.AreEqual(3.5f, worldPos.x);
            Assert.AreEqual(-3.5f, worldPos.y);
        }

        [Test]
        public void CoordinateMapper_WorldToGrid_MapsCorrectly()
        {
            bool valid = CoordinateMapper.WorldToGridPosition(new Vector3(-3.5f, 3.5f, 0), 8, 8, 1f, Vector3.zero, out int x, out int y);
            Assert.IsTrue(valid);
            Assert.AreEqual(0, x);
            Assert.AreEqual(0, y);

            valid = CoordinateMapper.WorldToGridPosition(new Vector3(3.5f, -3.5f, 0), 8, 8, 1f, Vector3.zero, out x, out y);
            Assert.IsTrue(valid);
            Assert.AreEqual(7, x);
            Assert.AreEqual(7, y);
        }
        
        [Test]
        public void CoordinateMapper_WorldToGrid_OutOfBounds_ReturnsFalse()
        {
            bool valid = CoordinateMapper.WorldToGridPosition(new Vector3(-5f, 5f, 0), 8, 8, 1f, Vector3.zero, out int x, out int y);
            Assert.IsFalse(valid);
        }
    }
}
#endif
