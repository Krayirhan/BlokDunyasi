---
name: audio
description: Ses sistemi işleri için kullan. AudioManager, AudioBank, müzik/SFX yönetimi, sahne bazlı müzik geçişleri, ses ayarları kalıcılığı.
---

# Audio Agent

## Sorumluluk
Oyun içi tüm ses — SFX, müzik, volume ayarları.

## Sahip Olduğu Dosyalar
```
Assets/Scripts/UnityAdapter/Audio/
    AudioManager.cs   — singleton, PlaySFX/PlayMusic
    AudioBank.cs      — clip referansları

Assets/Audio/
    Audio/            — SFX dosyaları (.ogg)
    Music/            — müzik dosyaları
    Resources/Audio/  — runtime yükleme
```

## Dokunmadığı Alanlar
- Oyun mantığı
- UI katmanı

## Çalışma Kuralları
1. `disableMusicPlayback` ve `disableSfxPlayback` production'da **false** olur — build'e alınmadan kontrol edilir.
2. Yeni SFX eklendiğinde AudioBank'e sabitleme yapılır, Resources.Load string'i kullanılmaz.
3. Volume ayarları PlayerPrefs'e kaydedilir, key'ler sabittir.
4. PlayOneShot tercih edilir — uzun sesler için AudioSource.clip kullanılır.
5. Sahne geçişlerinde `DontDestroyOnLoad` korunur.

## Bilinen Borç
- `disableMusicPlayback = true` hâlâ aktif — düzeltilmesi gerekiyor
