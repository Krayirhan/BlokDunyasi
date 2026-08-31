<div align="center">

<img src="StoreScreenshots/game-logo.png" alt="Blok Dünyası logo" width="256" />

# Blok Dünyası

### Renkli bloklar. Akıllı hamleler. Daha yüksek skor.

Unity 6 ile geliştirilen, mobil odaklı blok puzzle oyunu.

[![Unity 6](https://img.shields.io/badge/Unity-6-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Android-3DDC84?logo=android&logoColor=white)](https://www.android.com/)
[![License](https://img.shields.io/badge/License-Private-lightgrey)](#lisans)

</div>

<p align="center">
  <img src="StoreScreenshots/main-menu.webp" alt="Blok Dünyası ana menü" width="32%" />
</p>

## Oyun nedir?

Blok Dünyası’nda farklı şekillerdeki blokları 8×8 veya 10×10 oyun tahtasına yerleştir. Satır ve sütunları tamamla, tahtayı temizle, kombo zincirleri kur ve en yüksek skoru hedefle.

## Öne çıkanlar

| 🧩 Stratejik puzzle | 🎨 Canlı temalar | 📈 İlerleme sistemi |
| --- | --- | --- |
| Farklı blok şekilleri ve iki tahta boyutu ile her tur yeni bir plan gerektirir. | Renkli blok setleri, ahşap tema ve özelleştirilebilir arayüz varlıkları. | Kombo, görev, ödül, skor geçmişi ve liderlik tablosu altyapısı. |

| 📱 Mobil kontrol | 💾 Güvenli kayıt | 🔒 Hazır servis katmanı |
| --- | --- | --- |
| Dokunmatik sürükle-bırak ve masaüstü test akışı. | En iyi skor, ayarlar ve oyun durumu için kalıcı kayıt. | AdMob, Firebase Analytics/Crashlytics ve Google Play Games entegrasyonları. |

## Oyun içinden gerçek ekranlar

<p align="center">
  <img src="StoreScreenshots/gameplay-807.webp" alt="807 puanlık oyun ekranı" width="18%" />
  <img src="StoreScreenshots/gameplay-171.webp" alt="171 puanlık oyun ekranı" width="18%" />
  <img src="StoreScreenshots/challenge-807.webp" alt="807 puan challenge ekranı" width="18%" />
  <img src="StoreScreenshots/challenge-1000.webp" alt="1000 puan challenge ekranı" width="18%" />
  <img src="StoreScreenshots/challenge-relax.webp" alt="Rahatlatıcı puzzle ekranı" width="18%" />
</p>

Bu galeri, oyunun gerçek akışından alınmış mobil ekranları gösterir: ana menü, aktif tahta, skor hedefi ve farklı oyun modları. Yeni mağaza görselleri `StoreScreenshots/` altında tutulmalıdır.

## Proje mimarisi

```text
Assets/
├── Scenes/                 # MainMenu, OyunEkranı, Scores
├── Scripts/
│   ├── Core/               # Unity'den bağımsız oyun kuralları ve veri modelleri
│   ├── UnityAdapter/       # Oyun motoru, input, UI ve servis adaptörleri
│   ├── UI/                 # Reklam ve ortak UI bileşenleri
│   └── Systems/            # Uygulama yaşam döngüsü ve global servisler
├── Resources/              # Fontlar, UI kitleri ve runtime asset'leri
└── Images/                 # Temalar, arka planlar ve görsel içerik
```

Oyun kuralları `Core` içinde test edilebilir kalır; Unity’ye özel davranışlar `UnityAdapter` katmanında toplanır.

## Hızlı başlangıç

### Gereksinimler

- Unity 6
- Android Build Support (Android geliştiriyorsanız)
- Git LFS (büyük görsel ve font asset’leri için önerilir)

### Projeyi açma

1. Depoyu klonlayın.
2. Unity Hub’da bu repository kökünü seçin.
3. Unity’nin paketleri ve asset’leri import etmesini bekleyin.
4. `Assets/Scenes/MainMenu.unity` sahnesini açın.

```bash
git clone https://github.com/Krayirhan/BlokDunyasi.git
cd BlokDunyasi
```

### Production sahneleri

1. `Assets/Scenes/MainMenu.unity`
2. `Assets/Scenes/OyunEkranı.unity`
3. `Assets/Scenes/Scores.unity`

Sahne listesi için tek kaynak `ProjectSettings/EditorBuildSettings.asset` dosyasıdır.

## Test ve doğrulama

Core testlerini IDE veya Unity Test Runner üzerinden çalıştırabilirsiniz. Release öncesi:

```text
✓ MainMenu → OyunEkranı → Scores akışı çalışıyor
✓ 8×8 ve 10×10 tahta boyutları oynanabilir
✓ Kayıt/yükleme ve en yüksek skor tutarlı
✓ Reklam izinleri ve consent akışı test edildi
✓ Safe area farklı cihaz oranlarında doğrulandı
✓ Store build'de debug/per-frame loglar kapalı
```

Detaylı kontrol listeleri için [Docs/](Docs/) klasörüne bakın.

## Geliştirici rehberi

| Konu | Kaynak |
| --- | --- |
| UI sorumlulukları | [Docs/UIControllerResponsibilities.md](Docs/UIControllerResponsibilities.md) |
| İlerleme ve ödül sahipliği | [Docs/UIProgressRewardOwnership.md](Docs/UIProgressRewardOwnership.md) |
| Adapter bağımlılık grafiği | [Docs/AdapterDependencyGraph.md](Docs/AdapterDependencyGraph.md) |
| Safe area sözleşmesi | [Docs/SafeAreaContract.md](Docs/SafeAreaContract.md) |
| Gameplay layout otoritesi | [Docs/GameplayLayoutAuthority.md](Docs/GameplayLayoutAuthority.md) |
| Repository hijyeni | [Docs/RepoHygienePolicy.md](Docs/RepoHygienePolicy.md) |

## Katkı akışı

1. Yeni bir branch açın: `codex/<konu>` veya `feature/<konu>`.
2. Değişiklikleri küçük ve anlamlı commit’lere bölün.
3. Testleri çalıştırın ve `git status --short` çıktısını kontrol edin.
4. UI değişiklikleri için pull request’e ekran görüntüsü ekleyin.

## Lisans

Bu repository özel bir projedir. Kod, görseller, fontlar ve üçüncü taraf asset’ler için dağıtım veya yeniden kullanım hakları ayrıca kontrol edilmeden dışarı aktarılamaz.

<div align="center">

**Blok Dünyası** · Daha iyi hamle, daha yüksek skor.

</div>
