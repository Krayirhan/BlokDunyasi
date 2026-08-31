using BlockPuzzle.UnityAdapter.Configuration;
using BlockPuzzle.Core.Common;
using UnityEngine;

public static class UISettingsProfile
{
    public const string ThemeKey = "settings_theme";
    public const string LastAutomaticThemeKey = "settings_theme_last_auto";
    public const string ReduceMotionKey = "settings_reduce_motion";
    public const string HighContrastKey = "settings_high_contrast";
    public const string AccessibilityPaletteKey = SettingsKeys.AccessibilityPalette;

    public const int ThemeClassic = 0;
    public const int ThemeNight = 1;
    public const int ThemeVivid = 2;
    public const int ThemeWood = 3;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        ApplyProjectColorGradingPreferences();
    }

    public static int GetThemeId()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(ThemeKey, ThemeClassic), ThemeClassic, ThemeWood);
    }

    public static bool IsReduceMotionEnabled()
    {
        return PlayerPrefs.GetInt(ReduceMotionKey, 0) == 1;
    }

    public static bool IsHighContrastEnabled()
    {
        return PlayerPrefs.GetInt(HighContrastKey, 0) == 1;
    }

    public static bool IsAccessibilityPaletteEnabled()
    {
        return PlayerPrefs.GetInt(AccessibilityPaletteKey, 0) == 1;
    }

    public static void SetThemeId(int themeId)
    {
        PlayerPrefs.SetInt(ThemeKey, Mathf.Clamp(themeId, ThemeClassic, ThemeWood));
        PlayerPrefs.Save();
        ApplyProjectColorGradingPreferences();
    }

    public static int GetLastAutomaticThemeId()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(LastAutomaticThemeKey, -1), -1, ThemeWood);
    }

    public static void SetLastAutomaticThemeId(int themeId)
    {
        PlayerPrefs.SetInt(LastAutomaticThemeKey, Mathf.Clamp(themeId, -1, ThemeWood));
        PlayerPrefs.Save();
    }

    public static void SetReduceMotion(bool enabled)
    {
        PlayerPrefs.SetInt(ReduceMotionKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetHighContrast(bool enabled)
    {
        PlayerPrefs.SetInt(HighContrastKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyProjectColorGradingPreferences();
    }

    public static void SetAccessibilityPalette(bool enabled)
    {
        PlayerPrefs.SetInt(AccessibilityPaletteKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyProjectColorGradingPreferences();
    }

    public static Color[] GetPreferredBlockPalette(Color[] fallbackPalette)
    {
        var source = IsAccessibilityPaletteEnabled() ? GetAccessibilityPalette() : fallbackPalette;
        if (source == null || source.Length == 0)
            source = GetAccessibilityPalette();

        var palette = new Color[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            Color color = source[i];
            if (IsHighContrastEnabled())
            {
                color = Color.Lerp(color, Color.white, 0.08f);
                color.a = 1f;
            }

            palette[i] = color;
        }

        return palette;
    }

    public static void ApplyProjectColorGradingPreferences()
    {
        float saturationMultiplier = 1f;
        float valueMultiplier = 1f;
        float contrastMultiplier = 1f;

        switch (GetThemeId())
        {
            case ThemeClassic:
                saturationMultiplier = 0.92f;
                valueMultiplier = 0.98f;
                contrastMultiplier = 1.02f;
                break;
            case ThemeNight:
                saturationMultiplier = 1f;
                valueMultiplier = 0.94f;
                contrastMultiplier = 1.04f;
                break;
            case ThemeVivid:
                saturationMultiplier = 1.12f;
                valueMultiplier = 1.08f;
                contrastMultiplier = 1.08f;
                break;
            case ThemeWood:
                saturationMultiplier = 1.06f;
                valueMultiplier = 1.04f;
                contrastMultiplier = 1.10f;
                break;
        }

        if (IsHighContrastEnabled())
        {
            saturationMultiplier *= 0.98f;
            valueMultiplier *= 1.03f;
            contrastMultiplier *= 1.12f;
        }

        if (IsAccessibilityPaletteEnabled())
        {
            saturationMultiplier *= 0.96f;
            valueMultiplier *= 1.02f;
            contrastMultiplier *= 1.04f;
        }

        ProjectColorGrading.SetExternalUiAdjustments(
            saturationMultiplier,
            valueMultiplier,
            contrastMultiplier,
            enableOverride: true);
    }

    private static Color[] GetAccessibilityPalette()
    {
        return new[]
        {
            new Color(0.337f, 0.706f, 0.914f, 1f),
            new Color(0.902f, 0.624f, 0f, 1f),
            new Color(0f, 0.62f, 0.451f, 1f),
            new Color(0.941f, 0.894f, 0.259f, 1f),
            new Color(0f, 0.447f, 0.698f, 1f),
            new Color(0.835f, 0.369f, 0f, 1f),
            new Color(0.8f, 0.475f, 0.655f, 1f),
            new Color(0.6f, 0.6f, 0.6f, 1f)
        };
    }
}
