# Campaign Playbook

Son Guncelleme: 2026-05-22

## Kanitlar

- `marketing-agent/product-context.md`
- `marketing-agent/brand-memory.md`
- `marketing-agent/research-notes.md`
- `Assets/Scripts/UnityAdapter/Analytics/AnalyticsEventCatalog.cs`
- `Assets/Scripts/Systems/AnalyticsEventFileLogger.cs`
- `Assets/Resources/AdMobRuntimeConfig.asset`

## Gercek Proje Bulgulari

- Kampanyada guvenle kullanilabilecek urun deger onerileri:
  - block puzzle gameplay
  - score chase
  - combo satisfaction
  - daily challenge
  - leaderboard / high score rekabeti
  - 3 dil destegi
- Analytics event katalogunda su alanlar kampanya/funnel olcumune uygun:
  - `first_open`
  - `tutorial_started`
  - `tutorial_completed`
  - `best_score_updated`
  - `session_summary`
  - `mission_completed`
  - `continue_offer_shown`
  - `continue_clicked`
  - `continue_success`
- Ad lifecycle loglama kodu banner/interstitial/rewarded tarafini takip etmeye uygun.
- Interstitial pacing kurallari sert degil; bu, reklam agirligini mesajlarken "oyunu boğan reklam" algisindan kacinmak icin avantaj olabilir.
- Firebase tabanli leaderboard/account yapisi sosyal rekabet acilarini destekliyor.
- Daily challenge ve notifications retention bazli kampanyalari destekliyor.
- IAP/store tarafi teknik olarak var ama dolu urun catalog'u gorunmuyor; bu nedenle purchase-led kampanya dili su an zayif.

## Varsayimlar

- Varsayim: Ilk kampanya dalgasi Android odakli olmali; proje kanitlari en guclu sekilde Android'i isaret ediyor.
- Varsayim: Ilk kreatiflerde en iyi calisacak acilar:
  - score chase
  - combo payoff
  - daily challenge
- Varsayim: Turkiye ve global Ingilizce marketler icin ayri copy varyantlari gerekebilir.
- Varsayim: Korean market hedefleniyorsa ayrica lokal kreatif QA ve store adaptasyonu gerekir.

## Eksik Bilgiler

- Hedef CPI, ROAS veya CAC beklentileri
- Paid UA acilacak marketler
- Soft launch mi tam launch mi planlandigi
- Store listing'de hangi isim kullanilacagi
- Remove ads veya IAP release kapsaminda aktif olup olmayacagi

## Kampanya Kurallari

- Projede olmayan veya placeholder olan sistemler one cikarilmaz.
- Su claim'ler su an kullanilmaz:
  - genis skin catalog
  - seasonal event dolu takvimi
  - derin premium economy
  - aktif Google Play Games entegrasyonu
- Ilk dalga kampanya dili sade tutulur:
  - ne oynaniyor
  - neden tekrar acilir
  - neden skor kovalanir

## Oncelikli Kampanya Aciari

- `Score Chase`
  - Kanca: kendi rekorunu kir
  - Kanit: high score, leaderboard, share summary
- `Combo Satisfaction`
  - Kanca: tek hamlede buyuk clear ve combo hissi
  - Kanit: combo systems, VFX, score feedback
- `Daily Return`
  - Kanca: bugunun challenge'i ve geri donus ritmi
  - Kanit: daily challenge, notifications
- `Smart Casual`
  - Kanca: kolay gir, ustalikla skor buyut
  - Kanit: sade loop + tekrar oynanabilirlik

## Kanal Bazli Kurallar

- Paid UA:
  - ilk 2 saniyede gameplay netligi
  - metagame degil core loop
- ASO:
  - block puzzle, score, combo, challenge ekseni
- CRM:
  - rekor, combo, bugun tekrar dene tonu
- Social:
  - score paylasimi ve challenge rekabeti

## Sonraki Guncellemede Kontrol Et

- Store screenshot seti
- Approved naming
- Video capture pipeline
- Funnel dashboard kurulumlari
