---
name: meta
description: Meta-game sistemleri işleri için kullan. MissionManager, AchievementManager, DailyChallengeManager, RewardInventory, CosmeticTheme, mission/achievement UI panelleri, Google Play Games entegrasyonu, leaderboard.
---

# Meta Systems Agent

## Sorumluluk
Oyuncuyu oyunda tutan tüm progression sistemleri.

## Sahip Olduğu Dosyalar
```
Assets/Scripts/UnityAdapter/Meta/
    AchievementManager.cs
    RewardInventory.cs
    Missions/
        MissionDefinition.cs
        MissionManager.cs
    Cosmetics/
        CosmeticTheme.cs
    UI/
        MainMenuProgressionUI.cs
        MissionPanel.cs
        MissionUIItem.cs
        CosmeticsPanel.cs

Assets/Scripts/UnityAdapter/Social/
    LeaderboardManager.cs
    ScoreValidator.cs
    DailyChallengeManager.cs

Assets/Scripts/UnityAdapter/Auth/
    AuthManager.cs
    GooglePlayGamesManager.cs
    FirebaseManager.cs
```

## Dokunmadığı Alanlar
- Core oyun mantığı
- Monetization/IAP akışı
- Analytics event gönderimi (sadece tetikler)

## Çalışma Kuralları
1. Görev tamamlanması oyun motorundan event ile alınır, polling yapılmaz.
2. Leaderboard senkronizasyonu network hatasında sessizce atlanır.
3. ScoreValidator'ı bypass eden submit yasaktır.
4. FirebaseManager null-safe kullanılır — offline mod desteklenir.
5. Yeni achievement: tanım + UI + analytics event üçlüsü birlikte eklenir.
