using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor helper to quickly scaffold Auth UI elements (panel, buttons, status text)
// Creates named objects so you can replace visuals (images/sprites/text) manually afterwards.
public static class AutoSetupUI
{
    [MenuItem("BlokDunyasi/2-Tıkla UI Kur (Auth UI)")]
    public static void CreateAuthUI()
    {
        var canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene. Please open your MainMenu scene and make sure a Canvas exists.");
            return;
        }

        // Find or create AuthPanel under Canvas
        var root = canvas.gameObject.transform;
        var authPanel = root.Find("AuthPanel");
        if (authPanel != null)
        {
            Debug.Log("AuthPanel already exists. Aborting creation to avoid duplicates.");
            Selection.activeGameObject = authPanel.gameObject;
            return;
        }

        var panelGO = new GameObject("AuthPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGO.transform.SetParent(root, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.6f, 0.02f);
        panelRect.anchorMax = new Vector2(0.98f, 0.28f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panelGO.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.35f);

        // Create buttons (Guest, Play, SignOut)
        GameObject CreateButton(string name, string label)
        {
            var btnGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(panelGO.transform, false);
            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180, 48);

            var img = btnGO.GetComponent<Image>();
            img.color = new Color(1f, 0.65f, 0.0f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(btnGO.transform, false);
            var txt = textGO.GetComponent<Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var tr = textGO.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;

            return btnGO;
        }

        var b1 = CreateButton("Btn_GuestSignIn", "Guest");
        var b2 = CreateButton("Btn_PlaySignIn", "Play Sign-in");
        var b3 = CreateButton("Btn_SignOut", "Sign Out");

        // Position buttons vertically
        b1.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, 40);
        b2.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, 0);
        b3.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, -40);

        // Create status text
        var statusGO = new GameObject("Txt_AuthStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        statusGO.transform.SetParent(panelGO.transform, false);
        var statusText = statusGO.GetComponent<Text>();
        statusText.text = "Not signed in";
        statusText.color = Color.white;
        statusText.alignment = TextAnchor.MiddleLeft;
        statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var srt = statusGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.02f, 0.02f);
        srt.anchorMax = new Vector2(0.98f, 0.4f);
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

        // Create Auth_UI holder and add AuthUIController
        var authHolder = new GameObject("Auth_UI");
        var controllerType = Type.GetType("BlockPuzzle.UnityAdapter.Auth.AuthUIController, BlockPuzzleUnityAdapter");
        Component controller = null;
        if (controllerType != null)
            controller = authHolder.AddComponent(controllerType);
        else
        {
            // Try without assembly name
            controllerType = Type.GetType("BlockPuzzle.UnityAdapter.Auth.AuthUIController");
            if (controllerType != null) controller = authHolder.AddComponent(controllerType);
        }

        if (controller != null)
        {
            // wire fields via SerializedObject so Editor can modify them
            var so = new SerializedObject(controller);
            var guestProp = so.FindProperty("guestSignInButton");
            var playProp = so.FindProperty("playSignInButton");
            var signOutProp = so.FindProperty("signOutButton");
            var statusProp = so.FindProperty("statusText");

            guestProp.objectReferenceValue = panelGO.transform.Find("Btn_GuestSignIn")?.GetComponent<Button>();
            playProp.objectReferenceValue = panelGO.transform.Find("Btn_PlaySignIn")?.GetComponent<Button>();
            signOutProp.objectReferenceValue = panelGO.transform.Find("Btn_SignOut")?.GetComponent<Button>();
            statusProp.objectReferenceValue = statusGO.GetComponent<Text>();
            so.ApplyModifiedProperties();
        }

        // Create AuthManager if missing (so guest id exists immediately)
        var authMgrType = Type.GetType("BlockPuzzle.UnityAdapter.Auth.AuthManager, BlockPuzzleUnityAdapter");
        if (authMgrType == null) authMgrType = Type.GetType("BlockPuzzle.UnityAdapter.Auth.AuthManager");
        var existing = GameObject.FindObjectOfType(authMgrType);
        if (existing == null && authMgrType != null)
        {
            var amgo = new GameObject("AuthManager");
            amgo.AddComponent(authMgrType);
        }

        Selection.activeGameObject = panelGO;
        Debug.Log("Auth UI scaffold created under Canvas. Replace button images/text as you like.");
    }
}
