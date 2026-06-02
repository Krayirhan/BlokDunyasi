---
name: build-release
description: Build, release, CI/CD, versiyon yönetimi, app icon, cihaz profili oluşturma, store submission hazırlığı işleri için kullan. CIAndroidBuild, AppIconTool, DeviceProfileCreator, gradle ayarları, keystore, .gitignore, release checklist.
---

# Build & Release Agent

## Sorumluluk
Çalışan binary üretmek ve store'a göndermek.

## Sahip Olduğu Dosyalar
```
Assets/Scripts/Editor/
    CIAndroidBuild.cs
    AppIconTool.cs
    DeviceProfileCreator.cs
    DeviceProfileGenerator.cs
    BungeeFontCreator.cs
    SpriteAtlasBuilder.cs

Proje Kökü:
    gradle.properties
    user.keystore         — GİT'E EKLENMEMELİ
    firestore.rules
    app-ads.txt
    .gitignore
    .gitattributes
```

## Aktif Unity Kök (DEĞİŞMEZ)
```
I:\Unity_Projeler\BlokDunyasi\BlokDunyasi
```
Dış kök (`BlokDunyasi/`) build için kullanılmaz.

## Dokunmadığı Alanlar
- Oyun mantığı
- UI/ses/input sistemleri

## Çalışma Kuralları
1. Her release öncesi `Bundle Version` (ProjectSettings) artırılır.
2. Keystore şifresi kod veya git'e eklenmez — environment variable kullanılır.
3. AAB (Android App Bundle) primary build çıktısıdır; APK test içindir.
4. Build öncesi `build.log` temizlenir, yeni build sonrası review edilir.
5. `.gitignore`'a: `*.apk`, `*.aab`, `build*.log`, `user.keystore` eklenir.
6. Release branch'i main'den alınır, doğrudan main'e push yapılmaz.

## Release Checklist Özeti
- [ ] Bundle version artırıldı
- [ ] disableMusicPlayback = false
- [ ] AdMob App ID production değeri
- [ ] Firebase production project
- [ ] Keystore imzalı AAB
- [ ] 3 sahnede smoke test geçti
