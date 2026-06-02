using System;
using System.IO;
using System.Linq;
using UnityEditor;

public static class CIAndroidBuild
{
    [MenuItem("Build/Android/Build Debug APK")]
    public static void BuildDebugApk()
    {
        bool previousUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        try
        {
            PlayerSettings.Android.useCustomKeystore = false;
            BuildAndroidApk("Blok Dunyasi-dev.apk", BuildOptions.Development | BuildOptions.AllowDebugging);
        }
        finally
        {
            PlayerSettings.Android.useCustomKeystore = previousUseCustomKeystore;
        }
    }

    [MenuItem("Build/Android/Build Release APK")]
    public static void BuildReleaseApk()
    {
        BuildAndroidApk("Blok Dunyasi.apk", BuildOptions.None);
    }

    private static void BuildAndroidApk(string fileName, BuildOptions buildOptions)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        PlayerSettings.bundleVersion = "8";
        PlayerSettings.Android.bundleVersionCode = 8;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new Exception("Build settings icinde aktif scene bulunamadi.");

        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = buildOptions
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("Android build basarisiz oldu.");

        UnityEngine.Debug.Log($"APK olusturuldu: {outputPath}, options={buildOptions}");
    }
}
