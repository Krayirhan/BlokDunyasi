using BlockPuzzle.Core.Persistence;
using NUnit.Framework;

namespace BlockPuzzle.Core.Tests.Persistence
{
    [TestFixture]
    [Category("Unit")]
    public class GameSettingsTests
    {
        [Test]
        public void Validate_NewPreviewFlags_UpdateLegacyAliasesWhenDisabled()
        {
            var settings = new GameSettings
            {
                ShowPlacementHints = true,
                ShowValidPlacements = true,
                ShowPlacementPreview = false,
                ShowLineClearPreview = false
            };

            settings.Validate();

            Assert.IsFalse(settings.ShowPlacementPreview);
            Assert.IsFalse(settings.ShowLineClearPreview);
            Assert.IsFalse(settings.ShowPlacementHints);
            Assert.IsFalse(settings.ShowValidPlacements);
        }

        [Test]
        public void Validate_NewPreviewFlags_UpdateLegacyAliases()
        {
            var settings = new GameSettings
            {
                ShowPlacementPreview = false,
                ShowLineClearPreview = true,
                ShowPlacementHints = true,
                ShowValidPlacements = false
            };

            settings.Validate();

            Assert.IsFalse(settings.ShowPlacementPreview);
            Assert.IsTrue(settings.ShowLineClearPreview);
            Assert.IsFalse(settings.ShowPlacementHints);
            Assert.IsTrue(settings.ShowValidPlacements);
        }
    }
}
