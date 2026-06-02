using UnityEngine;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.UI.Localization
{
    /// <summary>
    /// DİL SISTEMI REFERANS KILAVUZUyou
    /// 
    /// Bu dosya örnek kod parçacıkları ve en iyi uygulamaları gösterir.
    /// 
    /// KISA ÖZET:
    /// ==========
    /// 1. Setup: Tools > Setup Language System
    /// 2. Metin Elemanlarına LocalizedText ekle
    /// 3. Görsel Elemanlara LocalizedImage ekle
    /// 4. Dil seçim butonlarına LanguageButton ekle
    /// 5. Inspector'da Türkçe/İngilizce içeriği konfigüre et
    /// 
    /// ÖRNEK KOD PARÇACIKLARI:
    /// ====================
    /// 
    /// Dil Değiştirme (Programatik):
    /// ----------------------------
    /// LanguageManager.Instance.CurrentLanguage = LanguageManager.Language.Turkish;
    /// LanguageManager.Instance.CurrentLanguage = LanguageManager.Language.English;
    /// 
    /// Dil Değişikliğini Dinleme:
    /// --------------------------
    /// public class MyComponent : MonoBehaviour, ILanguageListener
    /// {
    ///     private void OnEnable()
    ///     {
    ///         LanguageManager.Instance.Subscribe(this);
    ///     }
    ///
    ///     private void OnDisable()
    ///     {
    ///         LanguageManager.Instance.Unsubscribe(this);
    ///     }
    ///
    ///     public void OnLanguageChanged(LanguageManager.Language newLanguage)
    ///     {
    ///         Debug.Log($"Language changed to: {newLanguage}");
    ///     }
    /// }
    /// 
    /// INSPECTOR SETUP ÖRNEĞİ:
    /// ======================
    /// 
    /// Türkçe/İngilizce Yazılı Buton:
    /// 1. Canvas > Button_StartGame (GameObject)
    ///    ├─ Image (Button background)
    ///    ├─ Button component (Button)
    ///    ├─ LanguageButton component (LanguageButton)
    ///    └─ Text (TextMeshProUGUI)
    ///       ├─ LocalizedText component
    ///       │  ├─ Turkish Text: "Başla"
    ///       │  └─ English Text: "Start"
    /// 
    /// Görsel Gösterimi Değişecek Buton:
    /// 1. Canvas > Button_Settings (GameObject)
    ///    ├─ Image (Button background with sprite)
    ///    │  └─ LocalizedImage component
    ///    │     ├─ Turkish Sprite: (settings_turkish.png)
    ///    │     └─ English Sprite: (settings_english.png)
    ///    ├─ Button component
    ///    └─ LanguageButton component
    /// 
    /// DİL SEÇİM BUTONLARI:
    /// 1. Canvas > LanguageButtons (Panel)
    ///    ├─ Button_Turkish
    ///    │  ├─ Image
    ///    │  ├─ Button
    ///    │  ├─ LanguageButton (targetLanguage: Turkish)
    ///    │  └─ Text
    ///    │     └─ LocalizedText
    ///    │        ├─ Turkish Text: "Türkçe"
    ///    │        └─ English Text: "Turkish"
    ///    │
    ///    └─ Button_English
    ///       ├─ Image
    ///       ├─ Button
    ///       ├─ LanguageButton (targetLanguage: English)
    ///       └─ Text
    ///          └─ LocalizedText
    ///             ├─ Turkish Text: "İngilizce"
    ///             └─ English Text: "English"
    /// 
    /// KURULUM TOOLS MENÜSü:
    /// ====================
    /// 
    /// - Tools > Setup Language System
    ///   → Scene'e LanguageManager ekler
    /// 
    /// - Tools > Language System > Create Turkish Text Button
    ///   → Türkçe seçim butonu oluşturur
    /// 
    /// - Tools > Language System > Create English Text Button
    ///   → İngilizce seçim butonu oluşturur
    /// 
    /// DOSYA YAPISI:
    /// =============
    /// 
    /// Assets/Scripts/UnityAdapter/UI/Localization/
    /// ├─ LanguageManager.cs (Singleton Manager)
    /// ├─ LocalizedText.cs (TextMeshProUGUI lokalizasyonu)
    /// ├─ LocalizedImage.cs (Image/Sprite lokalizasyonu)
    /// ├─ LanguageButton.cs (Dil seçim butonu)
    /// ├─ LanguageSystemSetup.cs (Editor menüleri)
    /// └─ USAGE_GUIDE.txt (Bu dosya)
    /// 
    /// DİKKAT NOKTALAR:
    /// ===============
    /// 
    /// ✓ LanguageManager DontDestroyOnLoad() yapılır (singleton)
    /// ✓ Dil seçimi PlayerPrefs'e kaydedilir
    /// ✓ Uygulama başladığında kaydedilmiş dil otomatik yüklenir
    /// ✓ LocalizedText/Image otomatik subscribe olurlar
    /// ✓ Gereksiz alt-komponente ekli kalmasından endişe etme
    /// ✓ Sistemindeki tüm oyunlar için kullanılabilir
    /// 
    /// SADELIK PRENSIBI:
    /// =================
    /// 
    /// HER SEÇ ELEMANINA:
    /// - LocalizedText eklersen → otomatik değişir
    /// - LocalizedImage eklersen → görsel değişir
    /// - Inspector'da metinleri yapıştır → hepsi bu kadar!
    /// 
    /// Çok komplekse mi? HAYIR! En sade lokalizasyon sistemi:
    /// 1) Component ekle (3 saniye)
    /// 2) Türkçe/İngilizce yaz (2 saniye)
    /// 3) Bitti!
    /// 
    /// </summary>
    [System.Obsolete("Bu dosya sadece referans ve dokumentasyondur. Kodu çalıştırmaz.")]
    public class LocalizationReferenceGuide : MonoBehaviour
    {
        // Bu sınıf sadece editor notları içindir, runtime'da hiçbir şey yapmaz
    }
}
