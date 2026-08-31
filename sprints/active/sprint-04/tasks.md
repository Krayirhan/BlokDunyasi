# Sprint 04 — Tasks

### T-01 — PostScore: username boşsa users doc'tan oku
- Backlog ref: none
- Owner: `meta`
- Priority: P1
- Status: done
- Dependencies: none
- Files:
  - `Assets/Scripts/UnityAdapter/Social/FirebaseManager.cs`
- Acceptance:
  - [ ] `PostScore` çağrısında `Username` boş olsa bile `users` doc snapshot'ından `username` okunup `leaderboard_public`'a yazılıyor
- Verification:
  - [ ] `dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal` hatasız geçiyor

### T-02 — MirrorUsersScoreToPublicAsync: fallback patch'e username ekle
- Backlog ref: none
- Owner: `meta`
- Priority: P1
- Status: done
- Dependencies: none
- Files:
  - `Assets/Scripts/UnityAdapter/Social/FirebaseManager.cs`
- Acceptance:
  - [ ] Mirror fallback path, `users` snapshot'ından username okuyup `leaderboard_public`'a ekliyor
  - [ ] `isGuest` ve `providerId` alanları da yazılıyor (eski eksik dökümanlar güncelleniyor)
- Verification:
  - [ ] `dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal` hatasız geçiyor

### T-03 — AuthStateChanged: username yoksa Firestore'dan hydrate et
- Backlog ref: none
- Owner: `meta`
- Priority: P1
- Status: done
- Dependencies: none
- Files:
  - `Assets/Scripts/UnityAdapter/Social/FirebaseManager.cs`
- Acceptance:
  - [ ] Giriş yapılmış kullanıcı için `NormalizedUsername` boşsa `users` doc'tan username çekilip `SaveLocalUsername` ile kaydediliyor
  - [ ] Hydration sonrası `CreateUserInFirestore` çağrılıp `leaderboard_public` sync ediliyor
- Verification:
  - [ ] `dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal` hatasız geçiyor

### T-05 — HighScoreTableView: OnFirebaseInitialized event dinle
- Backlog ref: none
- Owner: `meta`
- Priority: P0
- Status: done
- Dependencies: none
- Files:
  - `Assets/Scripts/UnityAdapter/UI/HighScoreTableView.cs`
- Acceptance:
  - [ ] Firebase geç initialize olsa bile Scores sahnesi açıldığında leaderboard yükleniyor
  - [ ] 5 saniyelik `Task.Delay` döngüsü kaldırıldı, event-driven yaklaşım kullanılıyor
- Verification:
  - [ ] Grep: `OnFirebaseInitialized` 3 referans (subscribe, unsubscribe, handler)

### T-04 — Derleme doğrulama
- Backlog ref: none
- Owner: `meta`
- Priority: P1
- Status: done
- Dependencies: T-01, T-02, T-03
- Files:
  - `Assets/Scripts/UnityAdapter/Social/FirebaseManager.cs`
- Acceptance:
  - [ ] Adapter build hatasız tamamlanıyor
- Verification:
  - [ ] `dotnet build BlockPuzzleUnityAdapter.csproj -v:minimal`

### T-06 — Reklam cold-start ve yaşam döngüsü dayanıklılığı
- Backlog ref: none
- Owner: `monetization`
- Priority: P1
- Status: done
- Dependencies: none
- Files:
  - `Assets/Scripts/UI/Ads/*.cs`
  - `Assets/Scripts/UnityAdapter/Monetization/EntitlementManager.cs`
  - `Assets/Tests/UnityAdapter/*Ads*Tests.cs`
- Acceptance:
  - [x] Consent ve SDK callback kaybı timeout/retry ile toparlanıyor
  - [x] Reklam yükleme kilitleri timeout ve foreground dönüşünde iyileşiyor
  - [x] Remove Ads banner/interstitial yükleme ve gösterimini durduruyor
  - [x] Rewarded reklamlar ürün politikası gereği erişilebilir kalıyor
  - [x] Durum teşhisi cold-start zincirini gözlemlenebilir kılıyor
- Verification:
  - [x] Unity/C# derleme veya eşdeğer statik doğrulama
  - [ ] Reklam politika birim testleri
  - [x] `git diff --check` (değiştirilen dosyalar)
