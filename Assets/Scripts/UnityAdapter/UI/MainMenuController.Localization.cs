using System.Collections.Generic;
using TMPro;
using BlockPuzzle.UnityAdapter.UI.Localization;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.UI
{
    public partial class MainMenuController
    {
        private static class MainMenuLocalizationPresenter
        {
            public static void ApplyLanguageSpriteBindings(MainMenuController owner)
            {
                if (!owner.enableLanguageSpriteSwap)
                    return;

                var lang = LanguageManager.Instance.CurrentLanguage;

                if (lang == LanguageManager.Language.Korean)
                {
                    ApplySprite(owner.playButtonImage, owner.playButtonKoreanSprite != null ? owner.playButtonKoreanSprite : owner.playButtonEnglishSprite);
                    ApplySprite(owner.continueButtonImage, owner.continueButtonKoreanSprite != null ? owner.continueButtonKoreanSprite : owner.continueButtonEnglishSprite);
                    ApplySprite(owner.scoresButtonImage, owner.scoresButtonKoreanSprite != null ? owner.scoresButtonKoreanSprite : owner.scoresButtonEnglishSprite);
                    ApplySprite(owner.loginButtonImage, owner.loginButtonKoreanSprite != null ? owner.loginButtonKoreanSprite : owner.loginButtonEnglishSprite);
                    ApplySprite(owner.registerButtonImage, owner.registerButtonKoreanSprite != null ? owner.registerButtonKoreanSprite : owner.registerButtonEnglishSprite);
                    ApplySprite(owner.logoutButtonImage, owner.logoutButtonKoreanSprite != null ? owner.logoutButtonKoreanSprite : owner.logoutButtonEnglishSprite);

                    if (owner.logoKoreanObject != null)
                    {
                        if (owner.logoTurkishObject != null) owner.logoTurkishObject.SetActive(false);
                        if (owner.logoEnglishObject != null) owner.logoEnglishObject.SetActive(false);
                        owner.logoKoreanObject.SetActive(true);
                    }
                    else
                    {
                        ApplyLanguageObjectVisibility(owner.logoTurkishObject, owner.logoEnglishObject, true);
                        if (owner.logoKoreanObject != null) owner.logoKoreanObject.SetActive(false);
                    }
                }
                else
                {
                    bool isEnglish = lang == LanguageManager.Language.English;

                    ApplySprite(owner.playButtonImage, isEnglish ? owner.playButtonEnglishSprite : owner.playButtonTurkishSprite);
                    ApplySprite(owner.continueButtonImage, isEnglish ? owner.continueButtonEnglishSprite : owner.continueButtonTurkishSprite);
                    ApplySprite(owner.scoresButtonImage, isEnglish ? owner.scoresButtonEnglishSprite : owner.scoresButtonTurkishSprite);
                    ApplySprite(owner.loginButtonImage, isEnglish ? owner.loginButtonEnglishSprite : owner.loginButtonTurkishSprite);
                    ApplySprite(owner.registerButtonImage, isEnglish ? owner.registerButtonEnglishSprite : owner.registerButtonTurkishSprite);
                    ApplySprite(owner.logoutButtonImage, isEnglish ? owner.logoutButtonEnglishSprite : owner.logoutButtonTurkishSprite);

                    ApplyLanguageObjectVisibility(owner.logoTurkishObject, owner.logoEnglishObject, isEnglish);
                    if (owner.logoKoreanObject != null) owner.logoKoreanObject.SetActive(false);
                }
            }

            public static void SetupMenuLocalization(MainMenuController owner)
            {
                var localizedLabels = new HashSet<TextMeshProUGUI>();

                if (owner.playButton != null)
                    owner.SetupButtonLocalization(owner.playButton, "Basla", "Play", localizedLabels, "Play", "\uC2DC\uC791");

                if (owner.continueButton != null)
                    owner.SetupButtonLocalization(owner.continueButton, "Devam Et", "Continue", localizedLabels, "Continue", "\uACC4\uC18D\uD558\uAE30");

                if (owner.scoresButton != null)
                    owner.SetupButtonLocalization(owner.scoresButton, "Skorlar", "Scores", localizedLabels, "Scores", "\uC810\uC218");

                if (owner.vibrationToggleButton != null)
                {
                    var vibrationText = FindMenuButtonLabelText(owner.vibrationToggleButton);
                    if (vibrationText != null)
                        LocalizedTextSetup.SetupLocalization(vibrationText, "Titresim", "Vibration", "\uC9C4\uB3D9");
                }

                if (owner.verboseLogs)
                    Debug.Log("[MainMenuController] Menu localization setup completed.");
            }
        }
    }
}
