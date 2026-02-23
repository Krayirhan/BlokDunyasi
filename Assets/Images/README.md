# BLOK DÜNYASI - IMAGES FOLDER

Bu klasör, Blok Dünyası oyununda kullanılan sprite/texture dosyalarını içerir.

## 📁 Klasör Yapısı

Bu klasöre aşağıdaki sprite'ları eklemeniz gerekir:

### 🎨 Block Sprite'ları (8 adet)
- **Red Block** - Kırmızı blok sprite'ı 
- **Green Block** - Yeşil blok sprite'ı
- **Blue Block** - Mavi blok sprite'ı  
- **Yellow Block** - Sarı blok sprite'ı
- **Orange Block** - Turuncu blok sprite'ı
- **Purple Block** - Mor blok sprite'ı
- **Cyan Block** - Cyan blok sprite'ı
- **Pink Block** - Pembe blok sprite'ı

### 🔲 Grid Sprite'ları
- **Empty Cell** - Boş hücre sprite'ı (GridView için)

## 🔧 Unity Ayarları

Her sprite dosyası için Unity'de şu ayarları yapın:

1. **Import Settings**:
   - Texture Type: `Sprite (2D and UI)`
   - Sprite Mode: `Single`
   - Pixels Per Unit: `100`
   - Filter Mode: `Point (no filter)` (pixel art için)

2. **Inspector'da Atamalar**:
   - `DraggableBlockView` → Block Sprites (8 slot)
   - `GridView` → Cell Sprites (8 slot) + Empty Cell Sprite (1 slot)

## 🎮 Kullanım

- **DraggableBlockView**: Her slot farklı bir sprite kullanır (slotIndex % 8)
- **GridView**: Dolu hücreler rastgele sprite, boş hücreler emptyCellSprite kullanır
- **Fallback**: Sprite yoksa renk sistemi devreye girer

## 🖼️ Sprite Format Önerileri

- **Boyut**: 64x64 veya 128x128 pixel
- **Format**: PNG (transparency desteği için)
- **Style**: Blok Dünyası tarzı renkli kareler
- **Border**: İsteğe bağlı border/outline efektleri
