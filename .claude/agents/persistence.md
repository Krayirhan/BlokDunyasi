---
name: persistence
description: Kaydetme/yükleme, veri migration, istatistik depolama, best score yönetimi işleri için kullan. GameData, GameSaveManager, BestScoreStore, StatisticsManager, IStorageProvider, IGameStatePersistence, PlayerPrefs politikası, save versioning.
---

# Persistence Agent

## Sorumluluk
Oyun verisinin diske yazılması, okunması, migration'ı ve doğrulanması.

## Sahip Olduğu Dosyalar
```
Assets/Scripts/Core/Persistence/
    GameData.cs           — save format + migration
    BestScoreStore.cs     — en yüksek skor
    IGameStatePersistence.cs
    IStorageProvider.cs
    IJsonSerializer.cs

Assets/Scripts/UnityAdapter/Boot/
    GameSaveManager.cs    — async save orchestration
    StatisticsManager.cs  — istatistik batch yazma
    BestScoreManager.cs
```

## Dokunmadığı Alanlar
- Oyun mantığı (Core/Engine, Core/Board)
- UI gösterimi
- Analytics event gönderimi

## Çalışma Kuralları
1. Save format değiştiğinde `SaveVersion` artırılır, migration metodu eklenir.
2. Migration'lar zincir şeklinde: V(n-1)→V(n), atlama yapılmaz.
3. `ValidateAndSanitizeInPlace()` her migration sonrası çalışır.
4. IStorageProvider dışında doğrudan dosya I/O yasaktır.
5. Save data şifrelenmez — ama ScoreValidator'a hook edilir.
6. Async metodlar try-catch ile sarılır, hata loglanır, oyun durmaz.

## Kalite Hedefi
Migration coverage %100 — eski save'ler kayıpsız yüklenir.
