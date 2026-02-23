using UnityEngine;

public class SettingsScreenSetup : MonoBehaviour
{
    // Placeholder runtime helper. The Editor script (Assets/Editor/SettingsScreenSetup.cs)
    // generates the Settings scene and UI at edit-time. This runtime class prevents
    // compilation errors and provides a small helper message.

    void Start()
    {
        // No runtime creation here. Use the Editor menu to generate the scene.
    }

    public void OpenEditorCreateHint()
    {
        Debug.Log("To generate Settings scene use: BlokDunyasi > Setup > Create Settings Screen");
    }
}