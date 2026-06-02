---
name: monetization
description: Para kazanma sistemi işleri için kullan. StoreManager, ContinueEconomyManager, EntitlementManager, AdMobBootstrap, AdPolicyManager, ShopPanel, ShopItemUI, ProductDefinition, ödüllü reklam akışı, IAP satın alma akışı.
---

# Monetization Agent

## Sorumluluk
Reklamlar, in-app purchase, oyun devamı ekonomisi.

## Sahip Olduğu Dosyalar
```
Assets/Scripts/UnityAdapter/Monetization/
    StoreManager.cs
    ContinueEconomyManager.cs
    EntitlementManager.cs
    ProductDefinition.cs
    ShopPanel.cs
    ShopItemUI.cs

Assets/Scripts/UI/
    AdMobBootstrap.cs

Assets/Scripts/UnityAdapter/Analytics/
    AdTelemetry.cs
    AdPolicyManager.cs
    AdMobPrivacyOptionsPresenter.cs

Assets/Resources/
    AdMobRuntimeConfig.asset
```

## Dokunmadığı Alanlar
- Core oyun mantığı
- Leaderboard/social sistemler

## Çalışma Kuralları
1. Ödüllü reklam başarısız olursa fallback mekanizması (token kullanımı) devreye girer.
2. IAP restore işlemi her platforma özgü test edilir.
3. Consent alınmadan reklam gösterilmez — AdMobPrivacyOptionsPresenter zorunlu.
4. Ad Unit ID'ler AdMobRuntimeConfig.asset'te saklanır, kodda hardcoded olmaz.
5. ContinueEconomyManager token/reklam kararı oyun bitişinde, sahne geçişinden önce alınır.

## Bilinen Borç
- Ödüllü reklam reward mekanizması eksik implement — tamamlanması gerekiyor
