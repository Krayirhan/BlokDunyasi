# Product Context

Son Guncelleme: 2026-05-22

## Kanitlar

- `README.md`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/ProjectVersion.txt`
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/OyunEkrani.unity`
- `Assets/Scenes/Scores.unity`
- `Assets/Scripts/Core`
- `Assets/Scripts/UnityAdapter`
- `Assets/Scripts/UI/Ads/AdMobManager.cs`
- `Assets/Resources/AdMobRuntimeConfig.asset`
- `Assets/Resources/StarterPack_Product.asset`
- `Assets/Resources/Mission_FirstWin.asset`
- `Assets/Resources/SeasonalEventConfig.asset`
- `Assets/google-services.json`
- `app-ads.txt`

## Gercek Proje Bulgulari

- Aktif production Unity proje koku `d:\Unity_Projeler\BlokDunyasi\BlokDunyasi` klasorudur.
- Unity editor surumu `6000.2.14f1` olarak gorunuyor.
- Uygulama urun adi `BlokDunyasi` olarak tanimli.
- Android package name `com.krayirhanstudio.blokdunyasi`.
- Proje mobil oyun olarak tanimli:
  - `AndroidIsGame: 1`
  - minimum SDK 23
  - auto-rotate acik
- Production scene akisi tanimli:
  - `MainMenu`
  - `OyunEkrani`
  - `Scores`
- README ve core script yapisi oyunun bir `block puzzle` oldugunu dogruluyor.
- Oyun loop'u block placement, row/column clear, score progression ve combo mantigi etrafinda kurulu.
- Kodda dogrudan gorulen urun sistemleri:
  - best score persistence
  - game state persistence
  - combo scoring
  - tutorial event akisi
  - missions
  - cosmetics/themes
  - daily challenge
  - seasonal event config
  - remote tuning/config mantigi
  - leaderboard submission
  - score sharing
  - login/register/logout/guest benzeri account akislari
- Localization sistemi kodda uc dili destekliyor:
  - Turkish
  - English
  - Korean
- Main menu tarafinda dil bazli logo ve button sprite degisimi dusunulmus.
- Ad monetization altyapisi aktif olarak kodlanmis:
  - banner
  - interstitial
  - rewarded
- `AdMobRuntimeConfig.asset` icinde Android ad unit id'leri dolu.
- `AdMobRuntimeConfig.asset` kurallari:
  - startup'ta ads yukleniyor
  - interstitial game-over akisi icin aktif
  - minimum 2 tamamlanan session sonra interstitial
  - interstitial arasi minimum 180 saniye
  - session basi max 1 interstitial
- `app-ads.txt` dosyasi AdMob publisher iliskisini gosteriyor.
- Firebase entegrasyonlari projede mevcut:
  - Analytics
  - Auth
  - Crashlytics
  - Firestore
  - Remote Config paketleri
- Firebase config dosyasi Android icin tanimli ve package name ile eslesiyor.
- Hesap sistemi Firebase uzerinden username/password ve anonim/guest tabanli bir akisa sahip gorunuyor.
- `GooglePlayGamesManager` sinifi artik compatibility wrapper gibi davraniyor; yorum ve davranis mevcut Play Games entegrasyonunun aktif olmadigini gosteriyor.
- Public leaderboard yalniz anonim olmayan kullanicilar icin Firestore'da dusunulmus.
- `ShareSummaryUI` skor paylasim copy'si uretiyor.
- `DailyChallengeManager` ayni gun ayni seed mantigi ile calisiyor.
- `NotificationManager` Android local push reminder copy'leri uretiyor.
- Legal linkler tanimli:
  - privacy: `https://krayirhan.com/blockworld/privacy`
  - terms: `https://krayirhan.com/blockworld/terms`
- Resources altindaki monetization/liveops asset durumu:
  - `StarterPack_Product.asset` bos
  - `Mission_FirstWin.asset` bos
  - `SeasonalEventConfig.asset` bos
  - `DefaultTheme.asset` bos

## Varsayimlar

- Varsayim: README icinde `8x8 veya 10x10` ifadesi geciyor, ancak aktif production board boyutu bu taramada sahne veya runtime config uzerinden kesinlestirilmedi.
- Varsayim: `Scores` sahnesi hem high score hem leaderboard gosterimi icin kullaniliyor olabilir, ancak tam UX akisi bu taramada dogrudan acik degil.
- Varsayim: Kodda gecen `GameOver` ad placement ismi ayri sahne degil, gameplay icindeki panel/overlay olabilir.
- Varsayim: Daily challenge, missions, cosmetics ve seasonal event sistemleri urun vizyonunda var; ancak bazilarinin production icerigi henuz tamamlanmamis.

## Eksik Bilgiler

- Store listing'de kullanilacak kesin urun adi
- Ilk cikis ulkeleri ve hedef pazarlar
- Gercek IAP plani aktif mi, yoksa ilk faz yalnizca ads mi
- Uretimde kullanilacak leaderboard UI'nin final durumu
- Board size ve difficulty presentation'in store mesajina nasil cevrilecegi

## Sonraki Guncellemede Kontrol Et

- Aktif gameplay board size
- Store metadata ve screenshot paketi
- Rewarded continue UX metni
- Production leaderboard ekran akisi
