# Sprint Kuralları

> Bu dosya değiştirilemez. Sprint yapısını bozmak istersen önce bu kuralları güncelle ve gerekçeni yaz.

---

## 1. Sprint Anatomisi

Her sprint tam olarak şu 3 dosyadan oluşur:

```
sprints/active/sprint-XX/
├── brief.md    — amaç, scope, agent atamaları  (sprint başında yazılır)
├── tasks.md    — görev listesi + durum         (sprint boyunca güncellenir)
└── report.md   — sonuç, kalan borç             (sprint bitince yazılır)
```

Sprint klasör adı: `sprint-NN` (iki haneli, sıralı: sprint-01, sprint-02…)

---

## 2. Sprint Akışı (DEĞİŞTİRİLMEZ)

```
[Backlog] → brief.md yaz → Onay → tasks.md aç → Çalış → report.md yaz → [Arşiv]
```

1. **Brief** — Sprint başlamadan `brief.md` yazılır. Scope nettir, agent ataması vardır.
2. **Onay** — Brief insan tarafından onaylanır. Onaysız sprint başlamaz.
3. **Çalış** — `tasks.md` her görev tamamlandığında güncellenir.
4. **Report** — Sprint tamamlanınca `report.md` yazılır. Tamamlanmayan işler backlog'a döner.
5. **Arşiv** — `sprints/active/sprint-XX/` → `sprints/archive/sprint-XX/` taşınır.

---

## 3. Eş Zamanlı Sprint Kuralı

**Aktif sprint yalnızca 1 tane olur.**

`sprints/active/` içinde birden fazla sprint klasörü olamaz. Yeni sprint için aktif sprint önce kapatılır.

---

## 4. Agent Atama Kuralları

- Her task bir **primary agent**'a atanır.
- Bir task birden fazla agent gerektiriyorsa **lead agent** belirlenir, diğerleri destekler.
- Agent sınırları ihlal edilmez — örneğin `core-engine` agent UI dosyasına dokunmaz.

**Geçerli agentlar:**
| Agent | Sorumluluk |
|-------|-----------|
| `core-engine` | Pure C# oyun motoru |
| `persistence` | Save/load, migration |
| `ui-layout` | UI, canvas, responsive layout |
| `input` | Drag, dokunma, placement preview |
| `audio` | Ses sistemi |
| `meta` | Mission, achievement, leaderboard |
| `monetization` | Reklam, IAP, ekonomi |
| `build-release` | Build, CI, store |

---

## 5. Task Formatı

`tasks.md` içinde her task:

```
### T-NN — Başlık
- Agent: `agent-adı`
- Öncelik: P0 / P1 / P2
- Durum: todo / in-progress / done / blocked
- Bağımlılık: T-XX (varsa)
- Etkilenen dosyalar: path/to/file.cs
- Kabul kriteri: Ne yapılınca bitti sayılır
```

---

## 6. Öncelik Tanımları

| Öncelik | Anlam |
|---------|-------|
| P0 | Oyun çalışmıyor veya crash — sprint blokeri |
| P1 | Önemli özellik eksik veya ciddi bug |
| P2 | İyileştirme, refactor, borç ödeme |

P0 task varken P2 task başlatılamaz.

---

## 7. Tamamlanma Kriteri

Sprint "bitti" sayılır:
- [ ] Tüm P0 ve P1 task'lar `done`
- [ ] `report.md` yazıldı
- [ ] Tamamlanmayan P2'ler backlog'a taşındı
- [ ] `git commit` atıldı

---

## 8. Backlog Politikası

- `sprints/backlog.md` tek backlog kaynağıdır.
- Sprint sırasında ortaya çıkan yeni işler backlog'a eklenir, sprint scope'una giremez.
- Backlog önceliklendirmesi sprint başında yapılır.
