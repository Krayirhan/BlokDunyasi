using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

public class AddAdIdPermission : IPostGenerateGradleAndroidProject
{
    private const string AdIdPermission = "com.google.android.gms.permission.AD_ID";
    private const string PackagingOptionsContents =
        "android {\n" +
        "    packagingOptions {\n" +
        "        pickFirst \"META-INF/kotlinx_coroutines_core.version\"\n" +
        "    }\n" +
        "}\n";

    public int callbackOrder => 99;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        PatchManifest(Path.Combine(path, "src", "main", "AndroidManifest.xml"));

        string parent = Directory.GetParent(path)?.FullName;
        if (string.IsNullOrEmpty(parent))
        {
            return;
        }

        PatchManifest(Path.Combine(parent, "launcher", "src", "main", "AndroidManifest.xml"));
        PatchManifest(Path.Combine(parent, "unityLibrary", "src", "main", "AndroidManifest.xml"));

        string unityLibrary = Path.Combine(parent, "unityLibrary");
        EnsureGoogleMobileAdsPackagingOptions(unityLibrary);
        EnsureAndroidLibraryNamespace(
            Path.Combine(unityLibrary, "GoogleMobileAdsPlugin.androidlib", "build.gradle"),
            "com.google.unity.ads");
        EnsureAndroidLibraryNamespace(
            Path.Combine(unityLibrary, "AdMobAppIdManifest.androidlib", "build.gradle"),
            "com.krayirhanstudio.blokdunyasi.admobappid");
    }

    private static void EnsureGoogleMobileAdsPackagingOptions(string unityLibrary)
    {
        string pluginDirectory = Path.Combine(unityLibrary, "GoogleMobileAdsPlugin.androidlib");
        if (!Directory.Exists(pluginDirectory))
        {
            return;
        }

        string packagingOptionsPath = Path.Combine(pluginDirectory, "packaging_options.gradle");
        if (!File.Exists(packagingOptionsPath))
        {
            File.WriteAllText(packagingOptionsPath, PackagingOptionsContents);
            Debug.Log($"[AdMob Gradle] Eksik dosya olusturuldu: {packagingOptionsPath}");
        }
    }

    private static void EnsureAndroidLibraryNamespace(string buildGradlePath, string namespaceName)
    {
        if (!File.Exists(buildGradlePath))
        {
            return;
        }

        string contents = File.ReadAllText(buildGradlePath);
        if (contents.Contains("namespace "))
        {
            return;
        }

        const string androidBlock = "android {";
        int androidBlockIndex = contents.IndexOf(androidBlock, System.StringComparison.Ordinal);
        if (androidBlockIndex < 0)
        {
            Debug.LogWarning($"[AdMob Gradle] android blogu bulunamadi: {buildGradlePath}");
            return;
        }

        int insertionIndex = androidBlockIndex + androidBlock.Length;
        contents = contents.Insert(insertionIndex, $"\n    namespace \"{namespaceName}\"");
        File.WriteAllText(buildGradlePath, contents);
        Debug.Log($"[AdMob Gradle] Namespace eklendi: {namespaceName}");
    }

    private static void PatchManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return;
        }

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(manifestPath);

        XmlNode manifestNode = xmlDoc.SelectSingleNode("/manifest");
        if (manifestNode == null)
        {
            Debug.LogWarning($"[AD_ID] Manifest node bulunamadi: {manifestPath}");
            return;
        }

        if (HasAdIdPermission(manifestNode))
        {
            Debug.Log($"[AD_ID] Zaten mevcut: {manifestPath}");
            return;
        }

        XmlElement adIdPermission = xmlDoc.CreateElement("uses-permission");
        adIdPermission.SetAttribute("name", "http://schemas.android.com/apk/res/android", AdIdPermission);
        manifestNode.AppendChild(adIdPermission);
        xmlDoc.Save(manifestPath);

        Debug.Log($"[AD_ID] Izin manifest'e eklendi: {manifestPath}");
    }

    private static bool HasAdIdPermission(XmlNode manifestNode)
    {
        XmlNodeList usesPermissions = manifestNode.SelectNodes("uses-permission");
        if (usesPermissions == null)
        {
            return false;
        }

        foreach (XmlNode node in usesPermissions)
        {
            XmlAttribute nameAttribute = node.Attributes?["android:name"];
            if (nameAttribute != null && nameAttribute.Value == AdIdPermission)
            {
                return true;
            }
        }

        return false;
    }
}
