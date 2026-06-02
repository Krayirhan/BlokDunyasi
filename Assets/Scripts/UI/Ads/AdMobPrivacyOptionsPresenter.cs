using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BlockPuzzle.Core.Common;

/// <summary>
/// Settings sahnesinde privacy options formuna erisim saglayan basit bir buton sunar.
/// </summary>
public class AdMobPrivacyOptionsPresenter : MonoBehaviour
{
    private static AdMobPrivacyOptionsPresenter _instance;
    private Button _privacyButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_instance != null)
            return;

        GameObject presenterGO = new GameObject("AdMobPrivacyOptionsPresenter");
        _instance = presenterGO.AddComponent<AdMobPrivacyOptionsPresenter>();
        DontDestroyOnLoad(presenterGO);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.Equals(SceneCatalog.Settings, System.StringComparison.OrdinalIgnoreCase))
        {
            if (_privacyButton != null)
                _privacyButton.gameObject.SetActive(false);
            return;
        }

        EnsurePrivacyButton(scene);
        RefreshPrivacyButtonVisibility();
    }

    private void EnsurePrivacyButton(Scene scene)
    {
        if (_privacyButton != null)
            return;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas targetCanvas = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            var canvas = canvases[i];
            if (canvas == null || !canvas.gameObject.scene.IsValid() || canvas.gameObject.scene != scene)
                continue;

            targetCanvas = canvas;
            break;
        }

        if (targetCanvas == null)
            return;

        var buttonGO = new GameObject("PrivacyOptionsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(targetCanvas.transform, false);

        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(420f, 92f);
        rect.anchoredPosition = new Vector2(0f, 88f);

        var image = buttonGO.GetComponent<Image>();
        image.color = new Color(0.08f, 0.18f, 0.37f, 0.9f);

        _privacyButton = buttonGO.GetComponent<Button>();
        _privacyButton.targetGraphic = image;
        _privacyButton.onClick.AddListener(() =>
        {
            AdMobConsentManager.Instance.ShowPrivacyOptionsForm(_ => RefreshPrivacyButtonVisibility());
        });

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelGO.transform.SetParent(buttonGO.transform, false);

        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var text = labelGO.GetComponent<Text>();
        text.text = "Gizlilik Secenekleri";
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private void RefreshPrivacyButtonVisibility()
    {
        if (_privacyButton == null)
            return;

        bool shouldShow = AdMobConsentManager.Instance.IsPrivacyOptionsRequired;
        _privacyButton.gameObject.SetActive(shouldShow);
        _privacyButton.interactable = shouldShow;
    }
}
