# 🎮 Blok Dünyası

**Blok Dünyası**, Unity 6 ile geliştirilmiş bağımlılık yapan bir blok bulmaca oyunudur. Tetris ve Block Blast tarzı oyunlardan ilham alınarak tasarlanmıştır.

---

## 🎯 Oyun Hakkında

Blok Dünyası'nda amacınız, farklı şekillerdeki blokları 8x8 (veya 10x10) ızgaraya stratejik olarak yerleştirerek satır ve sütunları tamamlamaktır. Tamamlanan satır/sütunlar temizlenir ve puan kazanırsınız!

### ✨ Temel Özellikler

- 🧩 **Çeşitli Blok Şekilleri** - Tekli karelerden 3x3 kare bloklara kadar 20+ farklı şekil
- 🎨 **Renkli Görsel Tasarım** - 8 farklı renkte bloklar
- 📱 **Mobil Dostu** - Dokunmatik sürükle-bırak kontrolü (Unity New Input System)
- 💾 **Otomatik Kayıt** - Oyun durumu otomatik olarak kaydedilir
- 🏆 **En Yüksek Skor** - Rekorunuzu takip edin
- 🔥 **Kombo Sistemi** - Ardışık temizlemelerle çarpan bonusu

---

## 🕹️ Nasıl Oynanır?

1. **Blok Seç** - Ekranın altındaki 3 bloktan birini seçin
2. **Sürükle** - Bloğu parmağınızla (veya fare ile) ızgara üzerine sürükleyin
3. **Yerleştir** - Yeşil önizleme göründüğünde bırakın
4. **Temizle** - Satır veya sütun tamamlandığında otomatik temizlenir
5. **Tekrarla** - 3 blok bitince yeni set gelir

### 💡 İpuçları

- Köşeleri boş bırakmamaya çalışın
- Büyük blokları önce yerleştirin
- Birden fazla satır/sütunu aynı anda temizleyerek yüksek puan alın
- Kombo zinciri kurmaya çalışın!

---

## 🏗️ Teknik Yapı

### Mimari

Proje **Clean Architecture** prensiplerine uygun şekilde tasarlanmıştır:

```
┌──────────────────────────────────────────────┐
│                 Unity Layer                  │
│         (MonoBehaviours, UI, Input)         │
├──────────────────────────────────────────────┤
│              UnityAdapter Layer             │
│    (GameBootstrap, SimpleGridView, Input)   │
├──────────────────────────────────────────────┤
│                 Core Layer                   │
│    (GameEngine, PlacementEngine, Scoring)   │
│           ✨ Unity'den Bağımsız ✨          │
└──────────────────────────────────────────────┘
```

### Teknolojiler

| Teknoloji | Kullanım |
|-----------|----------|
| Unity 6 | Oyun motoru |
| C# 9+ | Programlama dili |
| New Input System | Dokunmatik/fare girişi |
| EnhancedTouch API | Mobil dokunmatik desteği |
| ScriptableObjects | Sprite ve ayar yönetimi |
| Assembly Definitions | Modüler derleme |

### Klasör Yapısı

```
Assets/
├── Scripts/
│   ├── Core/           # Unity'den bağımsız oyun mantığı
│   │   ├── Board/      # Tahta ve hücre yönetimi
│   │   ├── Engine/     # Oyun motoru ve yerleştirme
│   │   ├── Shapes/     # Blok şekilleri
│   │   ├── Rules/      # Puanlama kuralları
│   │   ├── RNG/        # Rastgele blok üretimi
│   │   └── Persistence/# Kayıt/yükleme sistemi
│   ├── UnityAdapter/   # Unity entegrasyonu
│   │   ├── Boot/       # GameBootstrap
│   │   ├── Input/      # Drag/Preview sistemleri
│   │   ├── Blocks/     # NewSimpleBlock, NewBlockTray
│   │   ├── Grid/       # SimpleGridView
│   │   └── UI/         # UIController, GameOverView
│   └── Editor/         # Editor araçları
├── Scenes/             # Oyun sahneleri
├── Prefabs/            # Prefab dosyaları
├── Resources/          # Runtime yüklenen dosyalar
└── Images/             # Sprite dosyaları
```

---

## 🎮 Puanlama Sistemi

| Eylem | Puan |
|-------|------|
| 1 satır temizleme | 10 puan |
| 2 satır aynı anda | 30 puan (1.5x çarpan) |
| 3 satır aynı anda | 60 puan (2x çarpan) |
| Kombo bonusu | Her ardışık temizlemede +0.5x |

---

## 🚀 Kurulum

### Gereksinimler

- Unity 6 (6000.x)
- New Input System Package
- TextMeshPro

### Başlangıç

1. Projeyi Unity Hub'dan açın
2. `Assets/Scenes/GameScene` sahnesini açın
3. Play tuşuna basın!

### Hızlı Kurulum (Editor Menüsü)

```
Blok Dünyası → Setup Game Scene
```

Bu menü otomatik olarak gerekli GameObject'leri oluşturur.

---

## 📱 Platform Desteği

- ✅ Windows / macOS / Linux
- ✅ Android
- ✅ iOS
- ✅ WebGL

---

## 🎨 Özelleştirme

### Sprite'ları Değiştirme

1. `Assets/BlockSpriteConfig` dosyasını seçin
2. Inspector'da sprite'ları sürükleyip bırakın
3. Oyunu çalıştırın

### Izgara Boyutunu Değiştirme

`GameBootstrap` bileşeninde `Board Size` değerini ayarlayın (8 veya 10 önerilir).

---

## 👨‍💻 Geliştirici Notları

- **Anchor Sistemi**: Her blok (0,0) hücresini merkez olarak kullanır
- **Preview Sistemi**: Geçerli yerleşim yeşil, geçersiz kırmızı gösterilir
- **Event-Driven**: `OnBlocksChanged`, `OnBoardChanged`, `OnScoreChanged` eventleri

---

*Blok Dünyası - Strateji ve Bulmaca Bir Arada!* 🧩✨
