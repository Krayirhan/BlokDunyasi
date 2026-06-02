# BlokDunyasi — Claude Çalışma Kılavuzu

**Proje:** Blok Dünyası (Krayirhan Studio)  
**Platform:** Android (iOS hazır)  
**Motor:** Unity 2022+ LTS, URP 2D  
**Aktif Unity Kökü:** `I:\Unity_Projeler\BlokDunyasi\BlokDunyasi`

---

## Agent Sistemi

Bu projede 8 özel agent tanımlıdır (`.claude/agents/`). Her agent kendi dosya sınırını aşmaz.

| Agent | Ne zaman çağırılır |
|-------|-------------------|
| `core-engine` | Pure C# oyun mantığı değişiklikleri |
| `persistence` | Save/load, migration, istatistik |
| `ui-layout` | UI, canvas, responsive layout, sahne setup |
| `input` | Drag, dokunma, placement preview |
| `audio` | Ses sistemi |
| `meta` | Mission, achievement, leaderboard, social |
| `monetization` | Reklam, IAP, ekonomi |
| `build-release` | Build, CI, store, versiyon |

---

## Sprint Sistemi

```
sprints/
├── _rules.md        — kurallar (değiştirilemez)
├── _template.md     — sprint şablonu
├── backlog.md       — tüm bekleyen işler
├── active/          — şu an çalışılan sprint (max 1)
│   └── sprint-XX/
│       ├── brief.md
│       ├── tasks.md
│       └── report.md
└── archive/         — tamamlanan sprintler
    └── sprint-XX/
```

**Yeni sprint başlatmak için:**
1. `sprints/backlog.md`'den task seç
2. `_template.md`'den kopyala → `sprints/active/sprint-NN/` oluştur
3. `brief.md` yaz, onay al
4. `tasks.md` aç, çalış
5. Bitince `report.md` yaz, `archive/`'a taşı

---

## Mimari Özet

```
Core/               → Platform-bağımsız oyun motoru (pure C#)
UnityAdapter/       → Unity entegrasyonu (MonoBehaviour'lar)
Systems/            → Uygulama başlatma (AppInitializer)
Editor/             → Unity Editor araçları
```

**Dependency yönü:** `UnityAdapter → Core` (ters yön yasaktır)

---

## Sabit Değerler (Değiştirilemez)

- Canvas reference: **1080 × 1920**, match **0.5**
- Aktif Unity kök: `I:\Unity_Projeler\BlokDunyasi\BlokDunyasi`
- Dış kök (`BlokDunyasi/`) build için **kullanılmaz**

---

## Bilinen Kritik Borçlar

1. `AudioManager.cs` — `disableMusicPlayback = true` (B-001, P0)
2. `ContinueEconomyManager.cs` — ödüllü reklam eksik (B-002, P0)
3. `GameBootstrap.cs` — 1674 satır God Object (B-003, P1)
4. `NewDragSystem.cs` — 817 satır, refactor gerekli (B-004, P1)

Tam liste: `sprints/backlog.md`

---

## Genel Kurallar

- Yorum satırı ekleme — kod kendini açıklamalı
- Hardcoded değer ekleme — DeviceLayoutProfile'a veya config'e taşı
- `using UnityEngine` — Core/ klasörüne giremez
- Magic number — const veya ScriptableObject field olur
- Breaking change — önce brief.md'de belirtilir, onay alınır
