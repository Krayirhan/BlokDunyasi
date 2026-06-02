using TMPro;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.UI.Localization
{
    public static class LocalizedFontUtility
    {
        private const string KoreanTmpFontResourcePath = "TMP/MalgunGothic_DynamicSDF";
        private const string KoreanLegacyFontResourcePath = "TMP/malgun";

        private static TMP_FontAsset _koreanTmpFont;
        private static Font _koreanLegacyFont;
        private static bool _fallbackRegistered;

        public static void ApplyForLanguage(LanguageManager.Language language)
        {
            if (language == LanguageManager.Language.Korean)
                EnsureKoreanFallbackRegistered();
        }

        public static TMP_FontAsset ResolveTmpFont(LanguageManager.Language language, TMP_FontAsset fallback)
        {
            if (language == LanguageManager.Language.Korean)
            {
                TMP_FontAsset koreanFont = GetKoreanTmpFont();
                if (koreanFont != null)
                    return koreanFont;
            }

            return fallback != null ? fallback : TMP_Settings.defaultFontAsset;
        }

        public static Font ResolveLegacyFont(LanguageManager.Language language, Font fallback)
        {
            if (language == LanguageManager.Language.Korean)
            {
                Font koreanFont = GetKoreanLegacyFont();
                if (koreanFont != null)
                    return koreanFont;
            }

            return fallback;
        }

        public static void EnsureFallback(TMP_FontAsset font)
        {
            TMP_FontAsset koreanFont = GetKoreanTmpFont();
            if (font == null || koreanFont == null || ReferenceEquals(font, koreanFont))
                return;

            if (font.fallbackFontAssetTable == null)
                font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

            if (!font.fallbackFontAssetTable.Contains(koreanFont))
                font.fallbackFontAssetTable.Add(koreanFont);
        }

        private static void EnsureKoreanFallbackRegistered()
        {
            if (_fallbackRegistered)
                return;

            EnsureFallback(TMP_Settings.defaultFontAsset);
            _fallbackRegistered = true;
        }

        private static TMP_FontAsset GetKoreanTmpFont()
        {
            if (_koreanTmpFont == null)
            {
                _koreanTmpFont = Resources.Load<TMP_FontAsset>(KoreanTmpFontResourcePath);

                // Validate the loaded asset — if atlas textures or material are null, it's broken
                if (_koreanTmpFont != null && !IsValidTmpFont(_koreanTmpFont))
                {
                    Debug.LogWarning("[LocalizedFontUtility] Loaded Korean TMP font has null atlas/material, recreating from source font.");
                    _koreanTmpFont = null;
                }

                // Fallback: create a dynamic TMP_FontAsset at runtime from the .ttf file
                if (_koreanTmpFont == null)
                {
                    Font legacyFont = GetKoreanLegacyFont();
                    if (legacyFont != null)
                    {
                        _koreanTmpFont = TMP_FontAsset.CreateFontAsset(legacyFont);
                        if (_koreanTmpFont != null)
                        {
                            _koreanTmpFont.name = "MalgunGothic_RuntimeSDF";
                            Debug.Log("[LocalizedFontUtility] Created Korean TMP font at runtime from malgun.ttf");
                        }
                    }
                }
            }

            return _koreanTmpFont;
        }

        private static bool IsValidTmpFont(TMP_FontAsset font)
        {
            if (font == null) return false;
            if (font.material == null) return false;

            // Check atlas textures array
            try
            {
                var atlasTextures = font.atlasTextures;
                if (atlasTextures == null || atlasTextures.Length == 0) return false;
                if (atlasTextures[0] == null) return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static Font GetKoreanLegacyFont()
        {
            if (_koreanLegacyFont == null)
                _koreanLegacyFont = Resources.Load<Font>(KoreanLegacyFontResourcePath);

            return _koreanLegacyFont;
        }
    }
}
