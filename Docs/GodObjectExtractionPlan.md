# God Object Extraction Plan

Projedeki god object'lerin parçalanma planı ve ilerleme durumu.
Her agent işe başlamadan önce bu dosyayı okuyarak hangi extraction'ların yapıldığını/yapılacağını görmelidir.

## İlerleme

| # | Çıkarılacak Sınıf | Kaynak | Tahmini Satır | Durum |
|---|---|---|---|---|
| 1 | **TutorialService** | GameBootstrap.cs | ~200 | ✅ **Tamam** |
| 2 | **AnalyticsTelemetryService** | GameBootstrap.cs | ~120 | ✅ **Tamam** |
| 3 | **TrayLayoutCalculator** | NewBlockTray.cs | ~200 | ✅ **Tamam** |
| 4 | **ContinueOfferController** | GameOverView.cs | ~500 | ✅ **Tamam** |
| 5 | **VisualBackgroundManager** | GameBootstrap.cs | ~200 | ✅ **Tamam** |

## Extraction Prensibi

1. Yeni sınıf aynı klasörde oluşturulur (non-MonoBehaviour safe C# sınıfı tercih edilir)
2. Tüm state ve method'lar taşınır
3. Kaynak sınıfta yalnızca delegasyon çağrısı kalır
4. Public static event'ler bridge ile korunur (geriye uyumluluk)
5. Davranış değişmez — birim test + build ile doğrulanır

## Detaylı Plan

### 1. ✅ TutorialService — TAMAM

**Dosyalar:**
- `Assets/Scripts/UnityAdapter/Boot/TutorialService.cs` (yeni)
- `Assets/Scripts/UnityAdapter/Boot/GameBootstrap.cs` (değişti)

**Taşınanlar:**
- `_isTutorialRunActive`, `_tutorialStepIndex`, `_pendingThreeByThreeSet`, `_threeByThreeSetApplied`
- `TutorialOpeningSet`, `TutorialThreeByThreeSet`
- `ShouldActivateTutorialForNewRun`, `ActivateTutorialRun`, `ApplyTutorialBlockSet`
- `UpdateTutorialProgress`, `ApplyPendingTutorialSpawnOverrideIfNeeded`
- `SkipActiveTutorial`, `CompleteTutorialRun`, `ResetTutorialRuntimeState`
- `EmitTutorialStepState` (lokalizasyon dahil)
- `ResolveGameOverGuidanceCode`'un tutorial branch'i

**GameBootstrap'te kalan:** Bridge event (`OnTutorialStepChangedFromService`), forwarding `SkipActiveTutorial()`, `MarkForActivation()` / `ActivateIfPending()` çağrıları

**Özel not:** Eski `TutorialController` (MonoBehaviour stub) ve `TutorialOverlayView` dokunulmadı — event üzerinden çalışmaya devam ediyorlar.

### 2. ✅ AnalyticsTelemetryService — TAMAM

**Dosyalar:**
- `Assets/Scripts/UnityAdapter/Boot/AnalyticsTelemetryService.cs` (yeni)
- `AnalyticsSessionContext` struct'ı `AnalyticsTelemetryService.cs` içinde tanımlıdır.
- `Assets/Scripts/UnityAdapter/Boot/GameBootstrap.cs` (değişti)

**Taşınanlar:**
- `EmitAnalyticsEvent` (~35 satır) — private
- `EmitGameplayTelemetry` (~80 satır) — public
- `TryGetScoreAnomalyCode` (~50 satır) — public static
- `AnalyticsSessionContext` struct (yeni)

**GameBootstrap'te kalan:** Bridge event (`OnAnalyticsEventFromService`), `RebuildAnalyticsContext()`, `_analyticsContext` field, `_analyticsService.EmitGameplayTelemetry(...)` çağrısı

**GameBootstrap satır:** ~1718 → ~1498 (TutorialService) → ~1364 (AnalyticsTelemetryService) → 1138 (VisualBackgroundManager)

### 3. ✅ TrayLayoutCalculator — TAMAM

**Kaynak:** `NewBlockTray.cs` (~1482 satır)

**Taşınacaklar:**
- `CalculateCanonicalLayout` (~130 satır)
- `GetShapeExtents`, `GetTrayY`
- `ShapeExtents` struct, `TrayLayoutResult` struct

**Dosyalar:**
- `Assets/Scripts/UnityAdapter/Blocks/TrayLayoutCalculator.cs` (yeni)
- `Assets/Scripts/UnityAdapter/Blocks/TrayLayoutConfig.cs` (struct, aynı dosyada)
- `Assets/Scripts/UnityAdapter/Blocks/NewBlockTray.cs` (değişti)

**Taşınanlar:**
- `ShapeExtents` struct
- `TrayLayoutResult` struct
- `CalculateCanonicalLayout` (~130 satır)
- `GetShapeExtents` (~35 satır)
- `GetTrayY` (~25 satır)

**NewBlockTray'de kalan:** `ApplyCanonicalLayout` (delegasyon), `ApplyLayoutResult`, `RefreshBackdropFromCurrentSlots`

**NewBlockTray satır:** ~1482 → ~1283 (**-199 satır**)

**Not:** Saf matematik — birim test edilebilir. `TrayLayoutConfig` struct'ı ile tüm bağımlılıklar parametre olarak geçilir.

### 4. ✅ ContinueOfferController — TAMAM

**Dosyalar:**
- `Assets/Scripts/UnityAdapter/UI/ContinueOfferController.cs` (yeni, 611 satır)
- `Assets/Scripts/UnityAdapter/UI/GameOverView.cs` (değişti, 2425 → 1851 satır)

**Taşınanlar:**
- Continue offer state machine: `_isActive`, `_waitingRewardedResult`, `_queuedRewardedAdShow`, `_rewardEarned`
- Tüm ad callback'leri: `HandleRewardedAdLoaded/Closed/Failed/UserEarned`
- Coroutine'ler: `ContinueCountdownRoutine`, `RewardedLoadTimeoutRoutine`
- UI: `SetContinueOfferVisible`, `SetContinueOfferAdUiVisible`
- Telemetri: `EmitContinueTelemetry` (reflection ile AdTelemetry)
- Lokalizasyon: continue mesajları (TrEn + Korece)
- ~20 method, ~15 field taşındı

**GameOverView'de kalan:** `_continueController` field, `OnFinalGameOverFromController`/`OnHideFromController` bridge event'leri, buton listener delegasyonu

**Mimari:** Non-MonoBehaviour sınıf. `FinalGameOverRequested`, `HideRequested`, `ContinueSucceeded` event'leri ile GameOverView'e bildirim. Constructor injection (UI refs, config, callback'ler).

### 5. ✅ VisualBackgroundManager — TAMAM

**Dosyalar:**
- `Assets/Scripts/UnityAdapter/Boot/VisualBackgroundManager.cs` (yeni, 284 satır)
- `Assets/Scripts/UnityAdapter/Boot/GameBootstrap.cs` (değişti, 1364 → 1138 satır)

**Taşınanlar (10 method, ~200 satır):**
- `NormalizeGameplayCamera`, `ApplyVisualReadability`, `CleanupDuplicates`
- `CleanupDuplicatesForName`, `ResolveLegacyOverlayBackground`
- `GetOrCreateBackdropRenderer`, `FindOrCreateBackdropRenderer`
- `ScaleRendererToCamera`, `GetGeneratedBackdropSprite`, `FindDeep`

**GameBootstrap'te kalan:** `_visualManager` field, delegasyon çağrıları

**Mimari:** Non-MonoBehaviour sınıf. Constructor'a `Transform` + tüm config değerleri geçilir.

## Boyut Raporu

| Dosya | Başlangıç | Şimdi | Fark |
|---|---|---|---|
| GameBootstrap.cs | 1718 | 1138 | **-580** |
| NewBlockTray.cs | 1482 | 1283 | **-199** |
| GameOverView.cs | 2425 | 1851 | **-574** |
| **Toplam** | **5625** | **4272** | **-1353** |

## Notlar

- Unity 6000.2.14f1 script derlemesi 2026-07-22 tarihinde başarılıdır (`Tundra build success`).
- Bu assembly Unity API'lerine ve `NoStandardLibraries` ayarına bağlı olduğu için normal `dotnet build` bağımsız doğrulama değildir.
- Saf/izole davranışlar için Unity Test Runner testleri; UI/ad akışları için ayrıca manuel smoke test gerekir.
- Extraction sırası: en izole olandan en bağımlı olana doğru.

## Doğrulama matrisi

| Extraction | Otomatik kapsam | Kalan doğrulama |
|---|---|---|
| TutorialService | Unity compile doğrulaması | İlk açılış, skip ve tamamlanma akışı |
| AnalyticsTelemetryService | Anomali kuralları ve event sayısı | Firebase/analytics gerçek cihaz doğrulaması |
| TrayLayoutCalculator | Extent ve responsive layout testleri | Farklı aspect ratio cihazlarda görsel kontrol |
| ContinueOfferController | Unity compile doğrulaması | Rewarded loaded/timeout/earned/failed akışları |
| VisualBackgroundManager | Unity compile doğrulaması | Sahne açılışı ve duplicate backdrop görsel kontrolü |
