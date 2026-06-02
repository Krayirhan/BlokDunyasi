# Sprint XX — [Başlık]

> Bu şablonu kopyala → `sprints/active/sprint-XX/` klasörüne yapıştır.
> brief.md, tasks.md, report.md olarak 3 ayrı dosyaya böl.

---

## brief.md şablonu

```markdown
# Sprint XX Brief — [Başlık]

**Tarih:** YYYY-MM-DD
**Tahmini süre:** X gün
**Lead agent:** [agent-adı]
**Destek agent(lar):** [agent-adı, ...]

## Hedef
[Bu sprint ne bitirince başarılı sayılır — 2-3 cümle]

## Scope
[Nelerin yapılacağı — madde madde]

## Scope Dışı
[Nelerin yapılmayacağı — kapsam kaymasını önler]

## Önkoşullar
[Başlamak için ne gerekiyor]

## Riskler
[Olası engeller]
```

---

## tasks.md şablonu

```markdown
# Sprint XX — Görev Listesi

Durum: 🔴 todo | 🟡 in-progress | ✅ done | ⛔ blocked

---

### T-01 — [Görev başlığı]
- Agent: `agent-adı`
- Öncelik: P1
- Durum: 🔴 todo
- Bağımlılık: —
- Etkilenen dosyalar:
  - `Assets/Scripts/.../Dosya.cs`
- Kabul kriteri:
  - [ ] Kriter 1
  - [ ] Kriter 2

---

### T-02 — [Görev başlığı]
- Agent: `agent-adı`
- Öncelik: P2
- Durum: 🔴 todo
- Bağımlılık: T-01
- Etkilenen dosyalar:
  - `Assets/Scripts/.../Dosya.cs`
- Kabul kriteri:
  - [ ] Kriter 1
```

---

## report.md şablonu

```markdown
# Sprint XX Report — [Başlık]

**Kapanış tarihi:** YYYY-MM-DD

## Özet
[Ne yapıldı — 3-5 cümle]

## Tamamlanan Tasklar
| Task | Durum | Not |
|------|-------|-----|
| T-01 | ✅ done | |
| T-02 | ✅ done | |

## Tamamlanamayan Tasklar
| Task | Neden | Backlog'a eklendi mi |
|------|-------|---------------------|
| T-03 | Zaman yetersizliği | ✅ |

## Ortaya Çıkan Yeni Borç
- [yeni borç 1]

## Bir Sonraki Sprinte Öneriler
- [öneri]
```
