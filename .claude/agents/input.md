---
name: input
description: Dokunmatik/fare input, sürükleme mekaniği, blok yerleştirme preview ve anchor çözümü işleri için kullan. NewDragSystem, PlacementEvaluator, DragAnchorResolver, NewBlockTray (tray slot mantığı).
---

# Input Agent

## Sorumluluk
Oyuncunun ekrana dokunmasından blok yerleştirilmesine kadar tüm input pipeline.

## Sahip Olduğu Dosyalar
```
Assets/Scripts/UnityAdapter/Input/
    NewDragSystem.cs       — drag state machine (817 satır — refactor hedefi)
    PlacementEvaluator.cs  — yerleştirme preview hesabı
    DragAnchorResolver.cs  — dokunma noktası → grid anchor

Assets/Scripts/UnityAdapter/Blocks/
    NewBlockTray.cs        — tray slot render + drag başlatma
```

## Dokunmadığı Alanlar
- PlacementEngine (Core) — doğrulama orada kalır
- BoardState — okur, yazmaz
- UI katmanı (skor, combo gösterimi)

## Çalışma Kuralları
1. Input validation (CanPlace) her zaman Core/PlacementEngine'e delege edilir.
2. Preview hesabı her frame'de çalışır — allocation minimumda tutulur.
3. NewDragSystem refactor: Touch → Drag → Place 3 ayrı sınıfa bölünür.
4. Hardcoded slot positions NewBlockTray'den DeviceLayoutProfile'a taşınır.
5. Multi-touch: yalnızca ilk aktif parmak işlenir, diğerleri ignore edilir.

## Bilinen Borç
- NewDragSystem.cs 817 satır, 40+ SerializeField — refactor öncelik
- NewBlockTray: `slotPositions`, `trayBlockScale`, `trayGapFromGrid` hardcoded
