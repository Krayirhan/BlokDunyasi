# 🎮 YENİ TEMİZ SİSTEM KURULUM REHBERİ

## 🔧 Tek Adımda Kurulum

Unity'de:

```
BlokDunyasi > Setup > 1. Setup New Clean System (FULL)
```

Bu menü:
1. Eski sistemleri devre dışı bırakır
2. Yeni componentleri sahneye ekler
3. Sahneyi kaydedilmek üzere işaretler

---

## 📋 Manuel Kurulum (Opsiyonel)

Eğer manuel yapmak isterseniz:

### Adım 1: Eski Sistemi Devre Dışı Bırak
```
BlokDunyasi > Setup > 2. Disable Old Systems Only
```

### Adım 2: Yeni Componentleri Ekle
```
BlokDunyasi > Setup > 3. Setup New Components Only
```

### Adım 3: Sahneyi Kaydet
`Ctrl+S`

---

## 🔄 Sistemler Arası Geçiş

Test etmek için eski/yeni sistem arasında geçiş yapabilirsiniz:

- **Yeni sisteme geç:** `BlokDunyasi > Setup > 5. Switch to NEW System`
- **Eski sisteme geç:** `BlokDunyasi > Setup > 6. Switch to OLD System`

---

## ✅ Sistem Durumunu Kontrol Et

```
BlokDunyasi > Setup > 4. Report Current System Status
```

Console'da hangi sistemin aktif olduğunu gösterir.

---

## 🏗️ Yeni Sistem Mimarisi

```
NewGameSystem (GameObject)
├── NewBlockTray      - Blokları gösterir
├── NewPreviewSystem  - Önizleme hücrelerini gösterir
└── NewDragSystem     - Sürükleme ve yerleştirme işler

Mevcut Sistemler (değişmedi):
├── GameBootstrap     - Oyun mantığı
└── SimpleGridView    - Grid gösterimi
```

---

## 🎯 Yeni Sistemin Çözdüğü Problemler

### ESKİ SİSTEMDE:
- Bloklar "center offset" ile oluşturuluyordu
- Pointer ≠ Anchor pozisyonu (karmaşık hesaplama)
- Preview ve placement farklı yerler gösteriyordu

### YENİ SİSTEMDE:
- Bloklar (0,0) anchor merkezli oluşturuluyor
- Pointer = Block pozisyonu = Anchor pozisyonu
- Hiç offset hesaplaması yok!

---

## 📁 Yeni Dosyalar

| Dosya | Açıklama |
|-------|----------|
| `NewSimpleBlock.cs` | Anchor-merkezli blok |
| `NewBlockTray.cs` | Yeni blok tray |
| `NewDragSystem.cs` | Temiz drag sistemi |
| `NewPreviewSystem.cs` | Temiz preview sistemi |
| `SetupNewSystem.cs` | Editor kurulum araçları |

---

## ⚠️ ÖNEMLİ NOTLAR

1. **Grid cellSize eşleşmeli:** NewBlockTray'deki `blockCellSize` ile SimpleGridView'daki `cellSize` aynı olmalı (varsayılan: 0.5)

2. **Sprite Config:** BlockSpriteConfig asset'ini NewBlockTray'e atayın (opsiyonel - yoksa varsayılan kare kullanılır)

3. **Test:** Oyunu çalıştırın, bir bloğu sürükleyin, preview'un grid hücrelerinin tam üstüne oturduğunu ve yerleştirmenin preview ile aynı yere olduğunu kontrol edin

---

## 🐛 Sorun Giderme

### "Block sürüklenmiyor"
- NewDragSystem'in aktif olduğundan emin olun
- Console'da hata var mı kontrol edin

### "Preview görünmüyor"
- NewPreviewSystem'in sahnede olduğundan emin olun
- Console'da `[NewPreviewSystem] Started preview` mesajını arayın

### "Bloklar yanlış yere yerleşiyor"
- SimpleGridView'ın sahnede olduğundan emin olun
- Cell size değerlerinin eşleştiğinden emin olun

---

## 🚀 Hızlı Test

1. Unity'de oyunu başlatın (Play)
2. Tray'deki bir bloğa tıklayın/dokunun
3. Grid üzerine sürükleyin
4. Yeşil preview hücrelerinin grid hücreleriyle hizalı olduğunu görün
5. Bırakın ve bloğun tam o hücrelere yerleştiğini doğrulayın

---

**Başarılar! 🎮**
