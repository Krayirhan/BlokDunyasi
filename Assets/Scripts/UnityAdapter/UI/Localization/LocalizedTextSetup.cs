using UnityEngine;
using TMPro;
using System.Collections.Generic;
using BlockPuzzle.UnityAdapter.UI.Localization;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// Metin bileşenlerine otomatik LocalizedText kurulumu yapan yardımcı sınıf
    /// </summary>
    public static class LocalizedTextSetup
    {
        public class LocalizationData
        {
            public TextMeshProUGUI TextComponent;
            public string TurkishText;
            public string EnglishText;
            public string KoreanText;

            public LocalizationData(TextMeshProUGUI text, string tr, string en)
            {
                TextComponent = text;
                TurkishText = tr;
                EnglishText = en;
                KoreanText = string.Empty;
            }

            public LocalizationData(TextMeshProUGUI text, string tr, string en, string ko)
            {
                TextComponent = text;
                TurkishText = tr;
                EnglishText = en;
                KoreanText = ko;
            }
        }

        /// <summary>
        /// Bir TextMeshProUGUI bileşenine LocalizedText ekler ve konfigüre eder
        /// </summary>
        public static void SetupLocalization(TextMeshProUGUI textComponent, string turkishText, string englishText, string koreanText = "")
        {
            if (textComponent == null)
            {
                Debug.LogWarning("[LocalizedTextSetup] TextComponent null!");
                return;
            }

            LocalizedText localizedText = textComponent.GetComponent<LocalizedText>();
            if (localizedText == null)
            {
                localizedText = textComponent.gameObject.AddComponent<LocalizedText>();
            }

            localizedText.Configure(textComponent, turkishText, englishText, koreanText, fallbackToObjectName: false);
        }

        public static void SetupLocalization(TextMeshProUGUI textComponent, string turkishText, string englishText)
        {
            SetupLocalization(textComponent, turkishText, englishText, "");
        }

        /// <summary>
        /// Birden fazla metin bileşenine aynı anda lokalizasyon kurulumu yapar
        /// </summary>
        public static void SetupMultiple(params LocalizationData[] items)
        {
            foreach (var item in items)
            {
                SetupLocalization(item.TextComponent, item.TurkishText, item.EnglishText, item.KoreanText);
            }
        }
    }
}
