# Research Notes

Son Guncelleme: 2026-05-22

## Kanitlar

- `README.md`
- `Assets/Scripts/UnityAdapter/UI/Localization/*`
- `Assets/Scripts/UnityAdapter/Social/*`
- `Assets/Scripts/UnityAdapter/Meta/*`
- `Assets/Scripts/UnityAdapter/Monetization/*`
- `Assets/Scripts/Systems/NotificationManager.cs`
- `Assets/Resources/*.asset`
- `Assets/Resources/TMP/*`

## Gercek Proje Bulgulari

- Urun mobil `block puzzle` kategorisinde.
- Gameplay loop skor, line clear, combo ve tekrar oynanabilirlik etrafinda kurulmus.
- Android odagi kuvvetli:
  - Android build ayarlari dolu
  - Android AdMob unit'leri tanimli
  - Android local notification akisi var
- Localization tarafinda dogrudan desteklenen diller:
  - Turkish
  - English
  - Korean
- Korean tarafinda ayrica font altyapisi (`Malgun`) ve dil bazli sprite/logo dusunulmus.
- Kullanici motivasyonu oldugu acikca gorulen urun yuzeyleri:
  - high score
  - combo
  - leaderboard
  - daily challenge
  - missions
  - continue offer
  - score sharing
  - account/login
- Social katman bulgulari:
  - Firestore public leaderboard mantigi var
  - score validation var
  - pending submission queue mantigi var
  - score share copy'si var
- Retention katmani bulgulari:
  - daily reminder push
  - combo reminder push
  - new features push
  - daily challenge
- Monetization katmani bulgulari:
  - banner
  - interstitial
  - rewarded
  - simulated/placeholder store products
- LiveOps katmani bulgulari:
  - remote config mantigi
  - seasonal event config mantigi
  - mission sistemi
  - reward scaling ve ad interval tuning alanlari
- Ancak liveops/monetization resources'in bir kismi placeholder durumda:
  - mission asset bos
  - product asset bos
  - theme asset bos
  - seasonal event asset bos

## Varsayimlar

- Varsayim: Rakip kategorisi `Woodoku`, `Block Blast`, `1010` benzeri skor odakli line-clear puzzle segmentine yakin.
- Varsayim: Cekirdek hedef oyuncu kisa session'larla skor kovalayan casual puzzle oyuncusu.
- Varsayim: Ikinci segment rekabet, leaderboard ve challenge motivasyonuna tepki veren oyuncular olabilir.
- Varsayim: Korean localization bir ticari pazar testi olabilir; proje icinden bunun kesin onceligi dogrulanamiyor.
- Varsayim: Guest-to-account conversion, retention ve social proof icin planlanan buyume eksenlerinden biri olabilir.

## Eksik Bilgiler

- Soft launch ulkeleri
- Korean localization'in gercek ticari hedef olup olmadigi
- Rakip benchmark listesi ekipte netlesmis mi
- Ads-only ekonomi mi yoksa hibrit ads+IAP mi hedefleniyor
- Store keyword stratejisi ve ASO hedef kelimeleri

## Sonraki Guncellemede Kontrol Et

- Store listing dili
- Keyword/ASO seti
- Firebase analytics dashboard event haritasi
- Paid UA kreatiflerinde hangi motivasyonun onde test edilecegi
