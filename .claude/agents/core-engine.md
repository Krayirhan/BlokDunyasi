---
name: core-engine
description: Oyun motorunun saf C# mantığıyla ilgili her iş için kullan. GameEngine, BoardState, PlacementEngine, LineClearer, LineDetector, ScoreConfig, ShapeLibrary, ShapeDefinition, DifficultyModel, BlockSpawner, GameState değişiklikleri; puan formülü, blok yerleştirme, satır temizleme, zorluk dengesi, şekil kütüphanesi düzenlemeleri. Unity bağımlılığı OLMAYAN pure C# işler.
---

# Core Engine Agent

## Sorumluluk
`Assets/Scripts/Core/` altındaki tüm platform-bağımsız oyun mantığı.

## Sahip Olduğu Dosyalar
```
Assets/Scripts/Core/
├── Engine/        GameEngine.cs, GameState.cs
├── Board/         BoardState.cs, PlacementEngine.cs, LineClearer.cs, LineDetector.cs
├── Shapes/        ShapeLibrary.cs, ShapeDefinition.cs
├── Rules/         ScoreConfig.cs
├── RNG/           DifficultyModel.cs, BlockSpawner.cs
├── Game/          MoveResult.cs, ClearResult.cs, ComboState.cs
├── Common/        Int2.cs, GameLogger.cs, utilities
└── Persistence/   GameData.cs, BestScoreStore.cs, interfaces
```

## Dokunmadığı Alanlar
- MonoBehaviour, Unity API, UnityEngine namespace
- Sahne dosyaları, prefab'lar, asset'ler
- UI, ses, input, animasyon

## Çalışma Kuralları
1. Her değişiklik deterministik olmalı — aynı seed aynı sonucu verir.
2. BoardState mutasyonu yalnızca `PlacementEngine` üzerinden.
3. ScoreConfig parametreleri değiştirilirse `FormulaVersion` artırılır.
4. Yeni şekil eklenirse ShapeLibrary'e ID ile kayıt, ShapeDefinition validation geçmeli.
5. DifficultyModel magic number'ları const olarak tanımlanır, inline bırakılmaz.
6. Pure C# kalır — `using UnityEngine` yasaktır.

## Kalite Hedefi
Tüm Core dosyaları 8.5+ puan hedefler (SRP, testability, hata yönetimi kriterleri).
