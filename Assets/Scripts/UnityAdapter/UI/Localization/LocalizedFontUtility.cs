using TMPro;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.UI.Localization
{
    public static class LocalizedFontUtility
    {
        private const string KoreanTmpFontResourcePath = "TMP/MalgunGothic_DynamicSDF";
        private const string KoreanLegacyFontResourcePath = "TMP/malgun";
        private const string PrimaryLatinTmpFontResourcePath = "TMP/GameFont SDF";
        private const string LatinLegacyFontResourcePath = "TMP/GameFont";

        private static TMP_FontAsset _koreanTmpFont;
        private static Font _koreanLegacyFont;
        private static TMP_FontAsset _latinTmpFont;
        private static Font _latinLegacyFont;
        private static bool _fallbackRegistered;

        public static void ApplyForLanguage(LanguageManager.Language language)
        {
            if (language == LanguageManager.Language.Korean)
                EnsureKoreanFallbackRegistered();
        }

        public static TMP_FontAsset GetDefaultLatinTmpFont()
        {
            if (_latinTmpFont == null)
            {
                _latinTmpFont = Resources.Load<TMP_FontAsset>(PrimaryLatinTmpFontResourcePath);

                if (_latinTmpFont != null && !IsValidTmpFont(_latinTmpFont))
                {
                    _latinTmpFont = null;
                }

                if (_latinTmpFont == null)
                {
                    Font legacyFont = Resources.Load<Font>(LatinLegacyFontResourcePath);

                    if (legacyFont != null)
                    {
                        _latinTmpFont = TMP_FontAsset.CreateFontAsset(legacyFont);
                        if (_latinTmpFont != null)
                        {
                            _latinTmpFont.name = "GameFont_RuntimeSDF";
                            EnsureFallback(_latinTmpFont);
                            Debug.Log("[LocalizedFontUtility] Created GameFont TMP font at runtime from GameFont.ttf");
                        }
                    }
                }
            }

            if (_latinTmpFont != null)
            {
                EnsureFallback(_latinTmpFont);
            }

            return _latinTmpFont;
        }

        public static TMP_FontAsset ResolveTmpFont(LanguageManager.Language language, TMP_FontAsset fallback)
        {
            if (language == LanguageManager.Language.Korean)
            {
                TMP_FontAsset koreanFont = GetKoreanTmpFont();
                if (koreanFont != null)
                    return koreanFont;
            }

            if (fallback != null && fallback != TMP_Settings.defaultFontAsset)
                return fallback;

            TMP_FontAsset latinFont = GetDefaultLatinTmpFont();
            if (latinFont != null)
                return latinFont;

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
