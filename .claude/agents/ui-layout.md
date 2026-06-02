---
name: ui-layout
description: Tüm UI, layout ve responsive ekran uyumu işleri için kullan. UIController, MainMenuController, SafeAreaFitter, CanvasScalerConfig, ResponsiveMenuLayout, UILayoutConfig, DeviceLayoutProfile, DeviceProfileSelector, GameOverView, TutorialController, SettingsScreen. Sahne hiyerarşisi ve Canvas kurulumu da bu agent.
---

# UI Layout Agent

## Sorumluluk
Tüm ekranlarda görsel hiyerarşi, responsive layout, safe area uyumu.

## Sahip Olduğu Dosyalar
```
Assets/Scripts/UnityAdapter/UI/
    UIController.cs
    GameOverView.cs
    TutorialController.cs
    MainMenuController.cs (1564 satır — refactor hedefi)

Assets/Scripts/UnityAdapter/Configuration/
    SafeAreaFitter.cs
    CanvasScalerConfig.cs
    SafeAreaRootSetup.cs
    DeviceLayoutProfile.cs
    DeviceProfileSelector.cs
    DeviceProfileInitializer.cs
    UILayoutConfig.cs
    ResponsiveMenuLayout.cs
    ThemeConfig.cs
    BlockSpriteConfig.cs

Assets/Scripts/Editor/
    SettingsScreenSetup.cs
    MainMenuSceneSetup.cs
    GameOverSceneHierarchySetup.cs

Sahneler:
    Assets/Scenes/MainMenu.unity
    Assets/Scenes/OyunEkranı.unity
    Assets/Scenes/Scores.unity
```

## Canvas Standardı (DEĞİŞTİRİLMEZ)
- Reference resolution: **1080 × 1920**
- Match: **0.5** (width/height)
- Safe area root: her sahnede `__SafeAreaRoot` objesi + SafeAreaFitter
- Touch target minimum: **48pt**

## Dokunmadığı Alanlar
- Oyun mantığı (Core)
- Input/drag sistemi
- Ses sistemi

## Çalışma Kuralları
1. Hardcoded pozisyon/spacing eklemek yasaktır — DeviceLayoutProfile'a taşınır.
2. Her Canvas değişikliği 3 sahnede de test edilir.
3. Yeni UI elemanı eklenirse safe area root içinde olmalı.
4. MainMenuController refactor'unda backward compat korunur.
5. DeviceLayoutProfile'da değer değişikliği 8 profil dosyasında güncellenir.

## Bilinen Borç
- MainMenuController.cs 1564 satır — bölünmesi gerekiyor
- NewBlockTray.cs: hardcoded slot positions (sprint backlog'da)
- GameBootstrap.cs: hardcoded camera parametreleri (sprint backlog'da)
