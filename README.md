# Blok Dunyasi

Blok Dunyasi, Unity 6 ile gelistirilmis bir mobil block puzzle oyunudur.

## Active Unity Project Root

Bu repository icinde acilmasi gereken tek production Unity proje koku:

```text
d:\Unity_Projeler\BlokDunyasi\BlokDunyasi
```

Unity Hub veya Unity Editor ile dogrudan bu klasor acilmalidir.

## Do Not Open Outer Root

Repository dis kokunde de `Assets/`, `Packages/` ve `ProjectSettings/` klasorleri bulundugu icin ikinci bir Unity projesi gibi gorunebilir. Ancak bu dis kok production build source-of-truth degildir.

Kanit:

- Dis kok dosyasi: `d:\Unity_Projeler\BlokDunyasi\ProjectSettings\EditorBuildSettings.asset`
- Gozlem: `m_Scenes: []`
- Ic kok dosyasi: `d:\Unity_Projeler\BlokDunyasi\BlokDunyasi\ProjectSettings\EditorBuildSettings.asset`
- Gozlem: production scene listesi yalniz burada tanimli

Bu nedenle build, scene acma ve sonraki teknik isler yalniz ic kok uzerinden yapilmalidir.

## Oyun Hakkinda

Blok Dunyasi'nda amac, farkli sekillerdeki bloklari 8x8 veya 10x10 izgara uzerine yerlestirerek satir ve sutunlari tamamlamaktir. Tamamlanan satir ve sutunlar temizlenir, puan kazanilir.

### Temel Ozellikler

- Cesitli blok sekilleri
- Mobil surukle-birak kontrolu
- Otomatik kayit
- En yuksek skor takibi
- Kombo sistemi

## UI Controller Responsibilities

UI controller facade split notes live in [Docs/UIControllerResponsibilities.md](Docs/UIControllerResponsibilities.md). `MainMenuController` remains the menu flow facade and `HudView` remains the gameplay HUD event facade; layout, localization and presentation logic are being pushed behind internal presenters without changing scene bindings.

## UI Progress Ownership

Progress, reward, mission, target and header ownership notes live in [Docs/UIProgressRewardOwnership.md](Docs/UIProgressRewardOwnership.md). The current contract is: `MainMenuProgressionUI` owns main-menu progression summary rendering, `TargetGoalSystem` owns gameplay target/progress rendering, `HudView` owns score/combo/status rendering, and `ProgressBarLayout` is kept only as a deprecated compatibility stub.

## Teknik Yapi

### Mimari

Proje katmanlari:

```text
Unity Layer
UnityAdapter Layer
Core Layer
```

### Teknolojiler

| Teknoloji | Kullanim |
|---|---|
| Unity 6 | Oyun motoru |
| C# | Programlama dili |
| New Input System | Dokunmatik/fare girisi |
| EnhancedTouch API | Mobil dokunmatik destegi |
| ScriptableObjects | Ayar ve asset yonetimi |

### Klasor Yapisi

```text
Assets/
  Scripts/
    Core/
    UnityAdapter/
    UI/
    Systems/
  Scenes/
  Prefabs/
  Resources/
```

## Kurulum

### Gereksinimler

- Unity 6
- New Input System package
- TextMeshPro

### How to Open the Project

1. Unity Hub ac
2. Su klasoru sec:

```text
d:\Unity_Projeler\BlokDunyasi\BlokDunyasi
```

3. Acilis sonrasi `Build Settings` veya `ProjectSettings/EditorBuildSettings.asset` kaynagini dogrula
4. Normal giris akisi icin `Assets/Scenes/MainMenu.unity` sahnesini ac

## Build Scenes

Production build scene listesi:

1. `Assets/Scenes/MainMenu.unity`
2. `Assets/Scenes/OyunEkrani.unity`
3. `Assets/Scenes/Scores.unity`

Not:
- Dosya sisteminde sahne adi Turkce karakter ile `OyunEkrani` yerine `OyunEkranı.unity` olarak durur.
- `EditorBuildSettings.asset` icinde de bu sahne production build listesinde yer alir.

## Release Source of Truth

- Aktif Unity proje koku: `BlokDunyasi/BlokDunyasi`
- Build settings kaynagi: `BlokDunyasi/ProjectSettings/EditorBuildSettings.asset`
- Aktif sahne klasoru: `BlokDunyasi/Assets/Scenes`
- Aktif script agaci: `BlokDunyasi/Assets/Scripts`

## Repo Hygiene

- Generated output ve local tooling policy icin: `Docs/RepoHygienePolicy.md`
- Test veya build sonrasinda `git status --short` kontrol edilmelidir.
- `Library`, `Temp`, `Logs`, `UserSettings`, `bin`, `obj`, `TestResults`, local `.apk/.aab` ve backup/debug klasorleri source olarak commitlenmemelidir.

## Release Logging

- Release log policy ve build-type log standardi icin: `Docs/ReleaseLoggingPolicy.md`
- Runtime info/verbose loglarin source-of-truth entry point'i `Assets/Scripts/Core/Common/GameLogger.cs` olarak kabul edilir.
- Store build'e giderken per-frame, per-drag, layout refresh ve gameplay spam loglari kapali olmalidir.

## Bootstrap Ownership

Projede iki ayri `GameBootstrap` sinifi vardir ve bunlar ayni sorumlulugu tasimaz:

1. Aktif gameplay bootstrap
   - Dosya: `Assets/Scripts/UnityAdapter/Boot/GameBootstrap.cs`
   - Namespace: `BlockPuzzle.UnityAdapter.Boot`
   - Sahne: `Assets/Scenes/OyunEkranı.unity`
   - Rol: production gameplay composition root

2. App-level runtime bootstrap
   - Dosya: `Assets/Scripts/Systems/GameBootstrap.cs`
   - Namespace: global namespace
   - Sahneye serialize edilmis gameplay bootstrap degildir
   - Rol: `AppInitializer` startup yolunu `BeforeSceneLoad` asamasinda tetiklemek

Production gameplay ownership her zaman `BlockPuzzle.UnityAdapter.Boot.GameBootstrap` sinifindadir.

## Adapter Dependency Graph

- Kritik Unity Adapter dependency contract'i icin: `Docs/AdapterDependencyGraph.md`
- Production path'te `GameBootstrap`, `NewDragSystem`, `NewBlockTray`, `SimpleGridView`, `HudView`, `GameOverView` ve `ResponsiveGameLayout` dependency'leri inspector/serialized reference ile acik baglanmalidir.
- `FindFirstObjectByType`, `Camera.main` ve benzeri runtime lookup'lar yalniz fallback olarak kabul edilir; kalici scene wiring yerine gecmemelidir.

## Safe Area Contract

- Build sahneleri icin safe-area owner map ve riskler: `Docs/SafeAreaContract.md`
- Release oncesi scene/device checklist: `Docs/SafeAreaValidationChecklist.md`
- MainMenu, OyunEkrani ve Scores sahneleri safe-area acisindan bu checklist'e gore dogrulanmadan store build alinmamalidir.

## Gameplay Layout Authority

- Oyun ekrani layout ownership matrisi: `Docs/GameplayLayoutAuthority.md`
- Gameplay screen validation checklist: `Docs/GameplayLayoutValidationChecklist.md`
- `ScreenLayoutManager`, `SimpleGridView`, `NewBlockTray`, `HudView` ve `GameOverView` rolleri bu dokumana gore degerlendirilmeden gameplay layout refactor'u yapilmamalidir.

## Verification Checklist

- Unity Hub dogru klasoru aciyor mu: `BlokDunyasi/BlokDunyasi`
- `ProjectSettings/EditorBuildSettings.asset` icinde 3 production sahnesi var mi
- `Assets/Scenes` altinda `MainMenu`, `OyunEkranı`, `Scores` mevcut mu
- `Assets/Scripts` altinda `Core`, `UnityAdapter`, `UI`, `Systems` yapisi mevcut mu
- Repository dis koku yanlislikla acilmadi mi
- Gameplay bootstrap ownership `UnityAdapter/Boot/GameBootstrap.cs` olarak biliniyor mu

## Developer Notes

- Anchor sistemi her blokta `(0,0)` hucreyi merkez kabul eder
- Preview sistemi gecerli ve gecersiz yerlestirmeyi ayri gosterir
- Olay bazli akis `OnBlocksChanged`, `OnBoardChanged`, `OnScoreChanged` eventleri uzerinden ilerler
- Project root warning: Yalniz `BlokDunyasi/BlokDunyasi` Unity projesi uzerinde calisin
