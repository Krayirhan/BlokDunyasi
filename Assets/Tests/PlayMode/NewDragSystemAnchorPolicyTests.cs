#if UNITY_INCLUDE_TESTS
using System.Reflection;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Input;
using NUnit.Framework;
using UnityEngine;

namespace BlockPuzzle.Tests.PlayMode
{
    public class NewDragSystemAnchorPolicyTests
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _createdObject;

        [Test]
        public void ValidPreviewAnchor_BecomesCommittedDropAnchor()
        {
            var dragSystem = CreateDragSystem();
            try
            {
                Invoke(dragSystem, "UpdateVisibleValidPreviewAnchor", new Int2(2, 3), true);

                var committed = TryGetCommittedAnchor(dragSystem, out var anchor);

                Assert.IsTrue(committed);
                Assert.AreEqual(new Int2(2, 3), anchor);
            }
            finally
            {
                Object.DestroyImmediate(_createdObject);
            }
        }

        [Test]
        public void ReleaseFrameJitter_DoesNotOverrideLastVisibleValidPreviewAnchor()
        {
            var dragSystem = CreateDragSystem();
            try
            {
                Invoke(dragSystem, "UpdateVisibleValidPreviewAnchor", new Int2(1, 1), true);
                Invoke(dragSystem, "SetCurrentPlacementCandidate", new Int2(4, 4), false);

                var committed = TryGetCommittedAnchor(dragSystem, out var anchor);

                Assert.IsTrue(committed);
                Assert.AreEqual(new Int2(1, 1), anchor);
            }
            finally
            {
                Object.DestroyImmediate(_createdObject);
            }
        }

        [Test]
        public void InvalidHoverAfterValidPreview_ClearsCommittedDropAnchor()
        {
            var dragSystem = CreateDragSystem();
            try
            {
                Invoke(dragSystem, "UpdateVisibleValidPreviewAnchor", new Int2(1, 2), true);
                Invoke(dragSystem, "UpdateVisibleValidPreviewAnchor", new Int2(5, 5), false);

                var committed = TryGetCommittedAnchor(dragSystem, out _);

                Assert.IsFalse(committed);
            }
            finally
            {
                Object.DestroyImmediate(_createdObject);
            }
        }

        [Test]
        public void NewValidPreview_ReplacesPreviousCommittedAnchor()
        {
            var dragSystem = CreateDragSystem();
            try
            {
                Invoke(dragSystem, "UpdateVisibleValidPreviewAnchor", new Int2(0, 0), true);
                Invoke(dragSystem, "UpdateVisibleValidPreviewAnchor", new Int2(3, 2), true);

                var committed = TryGetCommittedAnchor(dragSystem, out var anchor);

                Assert.IsTrue(committed);
                Assert.AreEqual(new Int2(3, 2), anchor);
            }
            finally
            {
                Object.DestroyImmediate(_createdObject);
            }
        }

        [Test]
        public void FinishDragState_ClearsCommittedPreviewAnchor()
        {
            var dragSystem = CreateDragSystem();
            try
            {
                Invoke(dragSystem, "UpdateVisibleValidPreviewAnchor", new Int2(2, 2), true);
                Invoke(dragSystem, "FinishDragState");

                var committed = TryGetCommittedAnchor(dragSystem, out _);

                Assert.IsFalse(committed);
            }
            finally
            {
                Object.DestroyImmediate(_createdObject);
            }
        }

        private NewDragSystem CreateDragSystem()
        {
            _createdObject = new GameObject("DragSystemTest");
            return _createdObject.AddComponent<NewDragSystem>();
        }

        private static bool TryGetCommittedAnchor(NewDragSystem dragSystem, out Int2 anchor)
        {
            object[] args = { null };
            bool committed = (bool)typeof(NewDragSystem)
                .GetMethod("TryGetCommittedDropAnchor", PrivateInstance)
                .Invoke(dragSystem, args);
            anchor = args[0] is Int2 value ? value : default;
            return committed;
        }

        private static object Invoke(NewDragSystem dragSystem, string methodName, params object[] args)
        {
            return typeof(NewDragSystem)
                .GetMethod(methodName, PrivateInstance)
                .Invoke(dragSystem, args);
        }
    }
}
#endif
