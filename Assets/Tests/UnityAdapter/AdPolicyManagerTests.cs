using BlockPuzzle.Core.Monetization;
using NUnit.Framework;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Tests
{
    public sealed class AdPolicyManagerTests
    {
        private const string RemoveAdsKey = "Entitlement_RemoveAds";

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(RemoveAdsKey);
        }

        [Test]
        public void AutomaticAds_AreAllowedWithoutEntitlement()
        {
            PlayerPrefs.DeleteKey(RemoveAdsKey);

            Assert.That(AdEntitlementPolicy.AllowAutomaticAds(EntitlementManager.IsRemoveAdsActive), Is.True);
            Assert.That(AdEntitlementPolicy.AllowRewardedAds(EntitlementManager.IsRemoveAdsActive), Is.True);
        }

        [Test]
        public void RemoveAds_BlocksAutomaticAdsButKeepsRewardedAvailable()
        {
            PlayerPrefs.SetInt(RemoveAdsKey, 1);

            Assert.That(EntitlementManager.IsRemoveAdsActive, Is.True);
            Assert.That(AdEntitlementPolicy.AllowAutomaticAds(EntitlementManager.IsRemoveAdsActive), Is.False);
            Assert.That(AdEntitlementPolicy.AllowRewardedAds(EntitlementManager.IsRemoveAdsActive), Is.True);
        }

        [Test]
        public void RecoveryPolicy_DetectsTimeoutAndCapsBackoff()
        {
            Assert.That(AdRecoveryPolicy.HasTimedOut(10f, 29.9f, 20f), Is.False);
            Assert.That(AdRecoveryPolicy.HasTimedOut(10f, 30f, 20f), Is.True);
            Assert.That(AdRecoveryPolicy.GetRetryDelaySeconds(1, 5f, 60f), Is.EqualTo(5f));
            Assert.That(AdRecoveryPolicy.GetRetryDelaySeconds(10, 5f, 60f), Is.EqualTo(60f));
        }
    }
}
