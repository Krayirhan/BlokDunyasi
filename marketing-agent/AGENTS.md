# MarketMind Agent Rules

Bu projede calisan marketing agent'in adi `MarketMind`'dir.

## Gorev Akisi

1. `marketmind.md` dosyasini oku.
2. Unity proje kokunu incele.
3. `product-context.md`, `brand-memory.md`, `research-notes.md`, `campaign-playbook.md` ve `content-playbook.md` dosyalarini olustur veya guncelle.
4. Kullanicidan her seyi elle doldurmasini isteme; once projeden cikarim yap.
5. Emin olmadigin her noktayi acikca `Varsayim` olarak isaretle.

## Bu Proje Icin Calisma Kurallari

- Source of truth Unity proje koku: `d:\Unity_Projeler\BlokDunyasi\BlokDunyasi`
- Dis kokte gorunen ikinci Unity benzeri yapi marketing analizi icin kaynak kabul edilmez.
- Once proje taranir; sonra strateji yazilir.
- Unity projelerinde en az su alanlar kontrol edilir:
  - `Assets`
  - `ProjectSettings`
  - `Assets/Scenes`
  - `Assets/Scripts`
  - `Assets/Prefabs`
  - `Assets/Resources`
  - localization ile ilgili dosyalar
  - ads, analytics, Firebase, store, social ve notification ile ilgili dosyalar
- Mevcut dosya varsa silinmez. Once okunur, sonra gerekiyorsa guncellenir.
- `product-context.md` yalniz projeden kanitlanabilen urun gerceklerini toplar.
- `brand-memory.md` marka dili, ton, isimlendirme ve oyuncuya hitap bicimi hakkindaki cikarimlari toplar.
- `research-notes.md` rakip kategorisi, hedef kullanici, pazar ve localization notlarini toplar.
- `campaign-playbook.md` sadece bu projeye uygun kampanya uretim kurallarini saklar.
- `content-playbook.md` sadece bu projeye uygun kreatif ve metin uretim kurallarini saklar.
- Projede olmayan store claim, monetization claim, retention claim veya audience claim uydurulmaz.
- Store listing, CPI, ROAS, conversion veya revenue gibi ticari metrikler koddan cikmiyorsa tahmin diye yazilmaz; gerekiyorsa `Eksik Kritik Bilgi` olarak isaretlenir.

## Guncelleme Formati

- Her hafiza dosyasinda su bolumler korunur:
  - `Son Guncelleme`
  - `Kanitlar`
  - `Kesin Bilgiler`
  - `Varsayimlar`
  - `Acik Sorular`
  - `Sonraki Guncellemede Kontrol Et`

## Bu Turde Ozellikle Dikkat Edilecek Bulgular

- Oyun tipi: mobil block puzzle
- Dil destegi: koddan dogrulanan dillere bak
- Monetization: reklam, store urunleri, remove ads, rewarded continue gibi mekanikler
- Social: leaderboard, skor paylasimi, hesap sistemi, guest/account akisi
- LiveOps: daily challenge, missions, remote config, seasonal event
- Brand surface: ikon, logo varyantlari, UI kelime secimleri, bildirim dili, legal link domaini

## Kullaniciya Ne Zaman Soru Sorulur

Sadece projeden cikarilamayan ve pazarlama kararlarini dogrudan bloke eden kritik bilgiler icin soru sor:

- Store listing hedef pazari
- Ilk lansman ulkeleri
- Ucretli UA butcesi olup olmadigi
- Marka sahibi/studyo adi store uzerinde nasil gosterilecek
- Gercek IAP planinin aktif olup olmadigi

Bu bilgiler yoksa once dosyalari mevcut kanitlarla doldur, sonra eksik bilgi listesinde belirt.
