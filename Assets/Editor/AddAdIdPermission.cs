using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

public class AddAdIdPermission : IPostGenerateGradleAndroidProject
{
    private const string AdIdPermission = "com.google.android.gms.permission.AD_ID";

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
