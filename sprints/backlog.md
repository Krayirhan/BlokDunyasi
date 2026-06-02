# Backlog

Tüm bekleyen işlerin tek kaynağı. Sprint planlanırken buradan seçilir.

Öncelik: 🔴 P0 | 🟠 P1 | 🟡 P2

---

## 🔴 P0 — Blokörler

### B-001 — disableMusicPlayback production'da kapalı
- Agent: `audio`
- Etkilenen: `Assets/Scripts/UnityAdapter/Audio/AudioManager.cs`
- Kabul kriteri: Production build'de müzik çalışıyor, flag kaldırıldı veya false yapıldı

### B-002 — Ödüllü reklam reward mekanizması eksik
- Agent: `monetization`
- Etkilenen: `Assets/Scripts/UnityAdapter/Monetization/ContinueEconomyManager.cs`
- Kabul kriteri: Reklam izlenince oyun devam ediyor, fail durumunda fallback çalışıyor

---

## 🟠 P1 — Önemli İşler

### B-003 — GameBootstrap.cs God Object bölünmesi
- Agent: `ui-layout` (lead), `persistence`, `core-engine`
- Etkilenen: `Assets/Scripts/UnityAdapter/Boot/GameBootstrap.cs` (1674 satır)
- Hedef bölünme:
  - `GameOrchestrator.cs` — oyun akışı
  - `GameTelemetryCollector.cs` — analytics event
  - `GameSaveCoordinator.cs` — save/load koordinasyonu
- Kabul kriteri: Her yeni sınıf <400 satır, mevcut oynanış değişmeden çalışıyor

### B-004 — NewDragSystem.cs bölünmesi
- Agent: `input`
- Etkilenen: `Assets/Scripts/UnityAdapter/Input/NewDragSystem.cs` (817 satır)
- Hedef bölünme:
  - `InputEventRouter.cs` — touch/mouse olayları
  - `DragVisualizer.cs` — ghost preview
  - `PlacementResolver.cs` — anchor + validation
- Kabul kriteri: Drag-drop oynanışı aynı, test edilebilirlik arttı

### B-005 — NewBlockTray hardcoded pozisyonları profil sistemine taşı
- Agent: `input`, `ui-layout`
- Etkilenen: `Assets/Scripts/UnityAdapter/Blocks/NewBlockTray.cs`
- Değerler: `slotPositions [(-2.5,-4)(0,-4)(2.5,-4)]`, `trayBlockScale=0.7f`, `trayGapFromGrid=0.4f`
- Kabul kriteri: Pozisyonlar DeviceLayoutProfile'dan okunuyor, 3 aspect ratio'da test geçti

### B-006 — ScreenLayoutManager hardcoded layout değerleri
- Agent: `ui-layout`
- Etkilenen: `ScreenLayoutManager.cs`
- Değerler: `headerSpace=2.8f`, `traySpace=4.2f`, `middleGap=0.8f`, `footerSpace=0.8f`
- Kabul kriteri: Tüm değerler DeviceLayoutProfile'dan okunuyor

### B-007 — GameBootstrap.cs hardcoded kamera parametreleri
- Agent: `ui-layout`
- Etkilenen: `Assets/Scripts/UnityAdapter/Boot/GameBootstrap.cs`
- Değerler: `cameraPosition=(0,0,-10)`, `minAdaptiveCameraSize=6f`, `maxAdaptiveCameraSize=14f`
- Kabul kriteri: Kamera parametreleri DeviceLayoutProfile'dan okunuyor

### B-008 — Safe area 3 sahnede deploy edilmedi
- Agent: `ui-layout`
- Etkilenen: `MainMenu.unity`, `OyunEkranı.unity`, `Scores.unity`
- Kod hazır: `SafeAreaFitter.cs`, `SafeAreaRootSetup.cs`, `CanvasScalerConfig.cs`
- Kabul kriteri: 3 sahnede `__SafeAreaRoot` + SafeAreaFitter mevcut, Canvas Scaler 1080×1920 / 0.5

### B-009 — MainMenuController.cs bölünmesi (1564 satır)
- Agent: `ui-layout`
- Etkilenen: `Assets/Scripts/UnityAdapter/UI/MainMenuController.cs`
- Kabul kriteri: Sorumluluklar ayrıldı, backward compat korundu

### B-010 — Save data anti-cheat hook
- Agent: `persistence`, `meta`
- Etkilenen: `ScoreValidator.cs`, `GameData.cs`
- Kabul kriteri: Save yüklenirken skor ScoreValidator'dan geçiyor

---

## 🟡 P2 — İyileştirmeler

### B-011 — BlockSpawner.cs magic number'ları sabitler haline getir
- Agent: `core-engine`
- Etkilenen: `Assets/Scripts/Core/RNG/BlockSpawner.cs`
- Değerler: `0.18f`, `0.6f`, `0.55f` ve diğerleri
- Kabul kriteri: Tüm threshold'lar const veya config ile tanımlı

### B-012 — DifficultyModel CircularBuffer ayrı dosyaya taşı
- Agent: `core-engine`
- Etkilened: `Assets/Scripts/Core/RNG/DifficultyModel.cs`
- Kabul kriteri: `CircularBuffer<T>` kendi dosyasında, DifficultyModel referans ediyor

### B-013 — GameEngine.cs ExecuteMove() refactor
- Agent: `core-engine`
- Etkilened: `Assets/Scripts/Core/Engine/GameEngine.cs`
- Sorun: ExecuteMove() 102 satır
- Kabul kriteri: ExecuteMove() <40 satır, private helper'lara bölündü

### B-014 — GameData.cs migration logic ayrı sınıfa
- Agent: `persistence`
- Etkilened: `Assets/Scripts/Core/Persistence/GameData.cs`
- Kabul kriteri: `GameDataMigrator.cs` ayrı dosya, migration chain testlenebilir

### B-015 — LineDetector.cs redundant double-check kaldır
- Agent: `core-engine`
- Etkilened: `Assets/Scripts/Core/Board/LineDetector.cs` satır 77-78, 91-92
- Kabul kriteri: Tek geçişte doğrulama, O(n) korunuyor

### B-016 — İkinci GameBootstrap sınıfını yeniden adlandır
- Agent: `build-release`
- Etkilened: `Assets/Scripts/Systems/GameBootstrap.cs`
- Sorun: `BlockPuzzle.UnityAdapter.Boot.GameBootstrap` ile isim çakışması
- Kabul kriteri: `Assets/Scripts/Systems/AppStartup.cs` olarak yeniden adlandırıldı

### B-017 — .gitignore'a build artefact'leri ekle
- Agent: `build-release`
- Etkilened: `.gitignore`
- Kabul kriteri: `*.apk`, `*.aab`, `build*.log`, `analytics_build.binlog`, `user.keystore` ignore listesinde

### B-018 — Uzun telefon desteği (9:20+ aspect ratio)
- Agent: `ui-layout`
- Etkilened: DeviceLayoutProfile assets, ScreenLayoutManager
- Profiller: `PhoneTall_9_21` ve `Phone_9_20` güncellenmeli
- Kabul kriteri: Samsung S24 Ultra'da board/tray çakışması yok

### B-019 — Ghost preview görünümü iyileştirme
- Agent: `input`, `ui-layout`
- Etkilened: `SimpleGridView.cs`, ghost overlay sistemi
- Sorun: Ghost preview siyah/çirkin görünüyor
- Kabul kriteri: Theme'e uygun yarı saydam ghost overlay
