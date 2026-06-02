using UnityEngine;
using BlockPuzzle.UnityAdapter.UI;

namespace BlokDunyasiTools
{
    public static class DeviceProfileGenerator
    {
        [UnityEditor.MenuItem("BlokDunyasi/Setup/Generate Device Profiles")]
        public static void GenerateProfiles()
        {
            string folderPath = "Assets/Resources/UI/DeviceProfiles";
            System.IO.Directory.CreateDirectory(folderPath);

            // Small Phone (480x800, 4.5" and below)
            var smallPhone = ScriptableObject.CreateInstance<DeviceLayoutProfile>();
            smallPhone.deviceType = DeviceLayoutProfile.DeviceType.SmallPhone;
            smallPhone.deviceName = "Small Phone (480x800)";
            smallPhone.minWidth = 400f;
            smallPhone.maxWidth = 540f;
            smallPhone.minHeight = 700f;
            smallPhone.maxHeight = 850f;
            smallPhone.minAspectRatio = 0.55f;
            smallPhone.maxAspectRatio = 0.68f;
            smallPhone.minDiagonalInches = 3.5f;
            smallPhone.maxDiagonalInches = 4.7f;
            smallPhone.verticalSpacing = 8f;
            smallPhone.horizontalSpacing = 12f;
            smallPhone.minScale = 0.54f;
            smallPhone.maxScale = 1f;
            smallPhone.logoSlotPaddingH = 8f;
            smallPhone.logoSlotPaddingV = 8f;
            smallPhone.badgeSlotPaddingH = 4f;
            smallPhone.badgeSlotPaddingV = 4f;
            smallPhone.playSlotPaddingH = 12f;
            smallPhone.playSlotPaddingV = 12f;
            smallPhone.authSlotPaddingH = 4f;
            smallPhone.authSlotPaddingV = 4f;
            UnityEditor.AssetDatabase.CreateAsset(smallPhone, $"{folderPath}/SmallPhone_480x800.asset");

            // Standard Phone Portrait (720x1280, 5")
            var phonePortrait = ScriptableObject.CreateInstance<DeviceLayoutProfile>();
            phonePortrait.deviceType = DeviceLayoutProfile.DeviceType.PhonePortrait;
            phonePortrait.deviceName = "Phone Portrait (720x1280)";
            phonePortrait.minWidth = 650f;
            phonePortrait.maxWidth = 800f;
            phonePortrait.minHeight = 1200f;
            phonePortrait.maxHeight = 1350f;
            phonePortrait.minAspectRatio = 0.55f;
            phonePortrait.maxAspectRatio = 0.65f;
            phonePortrait.minDiagonalInches = 4.5f;
            phonePortrait.maxDiagonalInches = 5.5f;
            phonePortrait.verticalSpacing = 10f;
            phonePortrait.horizontalSpacing = 14f;
            phonePortrait.minScale = 0.62f;
            phonePortrait.maxScale = 1f;
            phonePortrait.logoSlotPaddingH = 10f;
            phonePortrait.logoSlotPaddingV = 10f;
            phonePortrait.badgeSlotPaddingH = 6f;
            phonePortrait.badgeSlotPaddingV = 6f;
            phonePortrait.playSlotPaddingH = 14f;
            phonePortrait.playSlotPaddingV = 14f;
            phonePortrait.authSlotPaddingH = 6f;
            phonePortrait.authSlotPaddingV = 6f;
            UnityEditor.AssetDatabase.CreateAsset(phonePortrait, $"{folderPath}/PhonePortrait_720x1280.asset");

            // FHD Phone (1080x1920, 5.5")
            var phoneFHD = ScriptableObject.CreateInstance<DeviceLayoutProfile>();
            phoneFHD.deviceType = DeviceLayoutProfile.DeviceType.PhoneFHD;
            phoneFHD.deviceName = "Phone FHD (1080x1920)";
            phoneFHD.minWidth = 1000f;
            phoneFHD.maxWidth = 1150f;
            phoneFHD.minHeight = 1850f;
            phoneFHD.maxHeight = 2000f;
            phoneFHD.minAspectRatio = 0.55f;
            phoneFHD.maxAspectRatio = 0.59f;
            phoneFHD.minDiagonalInches = 5.0f;
            phoneFHD.maxDiagonalInches = 6.0f;
            phoneFHD.verticalSpacing = 12f;
            phoneFHD.horizontalSpacing = 16f;
            phoneFHD.minScale = 0.65f;
            phoneFHD.maxScale = 1f;
            phoneFHD.logoSlotPaddingH = 12f;
            phoneFHD.logoSlotPaddingV = 12f;
            phoneFHD.badgeSlotPaddingH = 8f;
            phoneFHD.badgeSlotPaddingV = 8f;
            phoneFHD.playSlotPaddingH = 16f;
            phoneFHD.playSlotPaddingV = 16f;
            phoneFHD.authSlotPaddingH = 8f;
            phoneFHD.authSlotPaddingV = 8f;
            UnityEditor.AssetDatabase.CreateAsset(phoneFHD, $"{folderPath}/PhoneFHD_1080x1920.asset");

            // Modern Phone (1080x2340+, 19.5:9)
            var phoneModern = ScriptableObject.CreateInstance<DeviceLayoutProfile>();
            phoneModern.deviceType = DeviceLayoutProfile.DeviceType.PhoneModern;
            phoneModern.deviceName = "Phone Modern (1080x2340)";
            phoneModern.minWidth = 1000f;
            phoneModern.maxWidth = 1150f;
            phoneModern.minHeight = 2300f;
            phoneModern.maxHeight = 2500f;
            phoneModern.minAspectRatio = 0.42f;
            phoneModern.maxAspectRatio = 0.50f;
            phoneModern.minDiagonalInches = 6.2f;
            phoneModern.maxDiagonalInches = 7.2f;
            phoneModern.verticalSpacing = 14f;
            phoneModern.horizontalSpacing = 16f;
            phoneModern.minScale = 0.70f;
            phoneModern.maxScale = 1f;
            phoneModern.logoSlotPaddingH = 12f;
            phoneModern.logoSlotPaddingV = 12f;
            phoneModern.badgeSlotPaddingH = 8f;
            phoneModern.badgeSlotPaddingV = 8f;
            phoneModern.playSlotPaddingH = 16f;
            phoneModern.playSlotPaddingV = 16f;
            phoneModern.authSlotPaddingH = 8f;
            phoneModern.authSlotPaddingV = 8f;
            UnityEditor.AssetDatabase.CreateAsset(phoneModern, $"{folderPath}/PhoneModern_1080x2340.asset");

            // Phone Landscape
            var phoneLandscape = ScriptableObject.CreateInstance<DeviceLayoutProfile>();
            phoneLandscape.deviceType = DeviceLayoutProfile.DeviceType.PhoneLandscape;
            phoneLandscape.deviceName = "Phone Landscape";
            phoneLandscape.minWidth = 1200f;
            phoneLandscape.maxWidth = 2500f;
            phoneLandscape.minHeight = 600f;
            phoneLandscape.maxHeight = 1200f;
            phoneLandscape.minAspectRatio = 1.5f;
            phoneLandscape.maxAspectRatio = 2.5f;
            phoneLandscape.minDiagonalInches = 4.5f;
            phoneLandscape.maxDiagonalInches = 7.0f;
            phoneLandscape.verticalSpacing = 16f;
            phoneLandscape.horizontalSpacing = 20f;
            phoneLandscape.minScale = 0.68f;
            phoneLandscape.maxScale = 1f;
            phoneLandscape.logoSlotPaddingH = 16f;
            phoneLandscape.logoSlotPaddingV = 12f;
            phoneLandscape.badgeSlotPaddingH = 10f;
            phoneLandscape.badgeSlotPaddingV = 8f;
            phoneLandscape.playSlotPaddingH = 18f;
            phoneLandscape.playSlotPaddingV = 14f;
            phoneLandscape.authSlotPaddingH = 10f;
            phoneLandscape.authSlotPaddingV = 8f;
            UnityEditor.AssetDatabase.CreateAsset(phoneLandscape, $"{folderPath}/PhoneLandscape.asset");

            // Tablet Small (1024x768, iPad mini)
            var tabletSmall = ScriptableObject.CreateInstance<DeviceLayoutProfile>();
            tabletSmall.deviceType = DeviceLayoutProfile.DeviceType.TabletSmall;
            tabletSmall.deviceName = "Tablet Small (1024x768)";
            tabletSmall.minWidth = 950f;
            tabletSmall.maxWidth = 1100f;
            tabletSmall.minHeight = 700f;
            tabletSmall.maxHeight = 850f;
            tabletSmall.minAspectRatio = 1.15f;
            tabletSmall.maxAspectRatio = 1.40f;
            tabletSmall.minDiagonalInches = 7.0f;
            tabletSmall.maxDiagonalInches = 8.0f;
            tabletSmall.verticalSpacing = 16f;
            tabletSmall.horizontalSpacing = 20f;
            tabletSmall.minScale = 0.78f;
            tabletSmall.maxScale = 1f;
            tabletSmall.logoSlotPaddingH = 16f;
            tabletSmall.logoSlotPaddingV = 16f;
            tabletSmall.badgeSlotPaddingH = 12f;
            tabletSmall.badgeSlotPaddingV = 12f;
            tabletSmall.playSlotPaddingH = 20f;
            tabletSmall.playSlotPaddingV = 18f;
            tabletSmall.authSlotPaddingH = 12f;
            tabletSmall.authSlotPaddingV = 12f;
            UnityEditor.AssetDatabase.CreateAsset(tabletSmall, $"{folderPath}/TabletSmall_1024x768.asset");

            // Tablet Standard (1920x1080)
            var tabletStandard = ScriptableObject.CreateInstance<DeviceLayoutProfile>();
            tabletStandard.deviceType = DeviceLayoutProfile.DeviceType.TabletStandard;
            tabletStandard.deviceName = "Tablet Standard (1920x1080)";
            tabletStandard.minWidth = 1850f;
            tabletStandard.maxWidth = 2000f;
            tabletStandard.minHeight = 1000f;
            tabletStandard.maxHeight = 1150f;
            tabletStandard.minAspectRatio = 1.70f;
            tabletStandard.maxAspectRatio = 2.00f;
            tabletStandard.minDiagonalInches = 10.0f;
            tabletStandard.maxDiagonalInches = 11.0f;
            tabletStandard.verticalSpacing = 18f;
            tabletStandard.horizontalSpacing = 24f;
            tabletStandard.minScale = 0.82f;
            tabletStandard.maxScale = 1f;
            tabletStandard.logoSlotPaddingH = 20f;
            tabletStandard.logoSlotPaddingV = 18f;
            tabletStandard.badgeSlotPaddingH = 14f;
            tabletStandard.badgeSlotPaddingV = 14f;
            tabletStandard.playSlotPaddingH = 24f;
            tabletStandard.playSlotPaddingV = 20f;
            tabletStandard.authSlotPaddingH = 14f;
            tabletStandard.authSlotPaddingV = 14f;
            UnityEditor.AssetDatabase.CreateAsset(tabletStandard, $"{folderPath}/TabletStandard_1920x1080.asset");

            // Tablet Large (2048x1536+, iPad Pro)
            var tabletLarge = ScriptableObject.CreateInstance<DeviceLayoutProfile>();
            tabletLarge.deviceType = DeviceLayoutProfile.DeviceType.TabletLarge;
            tabletLarge.deviceName = "Tablet Large (2048x1536)";
            tabletLarge.minWidth = 2000f;
            tabletLarge.maxWidth = 3000f;
            tabletLarge.minHeight = 1500f;
            tabletLarge.maxHeight = 2500f;
            tabletLarge.minAspectRatio = 1.20f;
            tabletLarge.maxAspectRatio = 1.40f;
            tabletLarge.minDiagonalInches = 11.5f;
            tabletLarge.maxDiagonalInches = 15.0f;
            tabletLarge.verticalSpacing = 20f;
            tabletLarge.horizontalSpacing = 28f;
            tabletLarge.minScale = 0.85f;
            tabletLarge.maxScale = 1f;
            tabletLarge.logoSlotPaddingH = 24f;
            tabletLarge.logoSlotPaddingV = 20f;
            tabletLarge.badgeSlotPaddingH = 16f;
            tabletLarge.badgeSlotPaddingV = 16f;
            tabletLarge.playSlotPaddingH = 28f;
            tabletLarge.playSlotPaddingV = 24f;
            tabletLarge.authSlotPaddingH = 16f;
            tabletLarge.authSlotPaddingV = 16f;
            UnityEditor.AssetDatabase.CreateAsset(tabletLarge, $"{folderPath}/TabletLarge_2048x1536.asset");

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log("[DeviceProfileGenerator] Device profiles generated successfully!");
            LogProfileInfo(smallPhone);
            LogProfileInfo(phonePortrait);
            LogProfileInfo(phoneFHD);
            LogProfileInfo(phoneModern);
            LogProfileInfo(phoneLandscape);
            LogProfileInfo(tabletSmall);
            LogProfileInfo(tabletStandard);
            LogProfileInfo(tabletLarge);
        }

        private static void LogProfileInfo(DeviceLayoutProfile profile)
        {
            Debug.Log(profile.ToString());
        }
    }
}
