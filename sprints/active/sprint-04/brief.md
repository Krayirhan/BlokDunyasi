# Sprint 04 — Leaderboard Username Yazma Düzeltmesi

## Hedef
Skorlar sahnesinde diğer oyuncuların listede görünmemesi sorununu kök nedenlerinden düzelt.

## Kök Nedenler

| # | Neden | Dosya |
|---|---|---|
| A | `PostScore` sırasında `Username` boşsa `leaderboard_public`'a `username` yazılmıyor | `FirebaseManager.cs` |
| B | `MirrorUsersScoreToPublicAsync` fallback path `username` içermiyor | `FirebaseManager.cs` |
| C | `AuthStateChanged`'da `NormalizedUsername` boşsa `CreateUserInFirestore` hiç çağrılmıyor; yeni cihazda kullanıcı profili sync edilmiyor | `FirebaseManager.cs` |

## Kapsam Dışı
- Firestore migration (eski kullanıcıları Firebase Console'dan güncelleme) → backlog
- `scoreOverride` / `weeklyScoreOverride` alanları → backlog
- Leaderboard UI refactor → backlog

## Lead Owner
`meta` (T-06 reklam dayanıklılığı için `monetization` ortak sahibi)

## Değiştirilecek Dosyalar
- `Assets/Scripts/UnityAdapter/Social/FirebaseManager.cs`
- `Assets/Scripts/UI/Ads/*.cs`
- `Assets/Scripts/UnityAdapter/Privacy/ConsentGate.cs`
- `Assets/Scripts/UnityAdapter/Monetization/EntitlementManager.cs`
