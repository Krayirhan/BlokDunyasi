# Store Screenshot Tool

Bu arac, Google Play icin tek tikla ekran goruntusu batch'i uretir.

## Menu

- `BlokDunyasi/Store/Generate Play Store Screenshots`
- `BlokDunyasi/Store/Open Last Screenshot Folder`

## Nasil Calisir

1. Acik sahnedeki degisiklikleri kaydetmenizi ister.
2. Gerekirse Play Mode'a girer.
3. Sirayla hedef cozumurluklere Game View'i ayarlar.
4. PNG ekran goruntulerini klasore yazar.
5. Batch bitince (arac Play Mode'u actiysa) Play Mode'dan cikar.
6. Cikti klasorunu acar.

## Varsayilan Hedefler

- `phone/phone_01` -> `1080x1920` (9:16)
- `phone/phone_02` -> `1170x2080` (9:16)
- `tablet7/tablet7_01` -> `1260x2240` (9:16)
- `tablet10/tablet10_01` -> `1440x2560` (9:16)

## Cikti Konumu

- `StoreScreenshots/batch_YYYYMMDD_HHMMSS/...`

## Notlar

- Arac, dosya boyutunu ve cozumurlugu kontrol eder.
- Bir dosya 8 MB ustune cikarsa Console'da warning verir.
- Farkli set/cozumurluk icin `Assets/Editor/StoreScreenshotTool.cs` icindeki `Targets` dizisini duzenleyin.
