# Task Management — Özellik Spesifikasyonu

## Durum: Domain tamamlandı, Application geliştiriliyor

## Kesinleşen Kurallar
- Bir görev (PlanningTask) iki durumdan birindedir:
  - Havuzda (backlog): belirli bir güne atanmamış (`AssignedDate == null`).
  - Plana atanmış: belirli bir güne (`AssignedDate`) atanmış.
- Bir görev SADECE plana atanmışken ve mikro (günlük) görünümdeyken
  tamamlanabilir; havuzdayken veya haftalık (makro) görünümdeyken
  `Complete()` çağrılamaz — bu Domain seviyesinde bir invariant'tır.
- Görev atandığı gün (`AssignedDate`, gün bazlı) ile hatırlatma saati
  (`ReminderTime`, opsiyonel saat bazlı) birbirinden ayrı tutulur.
  `ReminderTime` sadece bir güne atanmış görevlerde set edilebilir;
  havuza dönen bir görevin hatırlatıcısı da temizlenir.
- Havuz ↔ Plan arası geçiş iki yönlüdür (`AssignToDate()` / `ReturnToPool()`),
  sürükle-bırak özelliği bunu gerektirir.
- Görev başlığı düzenlenebilir (`Rename()`), ancak tamamlanmış bir görevin
  başlığı değiştirilemez.
- Her görevin bir `Priority` (TaskPriority: Normal/Important/Critical) değeri
  vardır, oluşturulurken zorunlu parametre olarak verilir (varsayılan değer
  YOK — her görev için bilinçli bir seçim yapılmalı).
- Her görev bir kullanıcıya aittir (`UserId`, oluşturulurken verilir,
  değişmez). Bir kullanıcı sadece kendi görevlerini görür ve değiştirir.
- Görev oluşturma idempotent'tir: aynı kullanıcı için aynı `Id` ile tekrar
  gelen `CreateTaskCommand` yeni kayıt oluşturmaz, mevcut görevin Id'sini
  döner (offline kuyruğun tekrar denemesi için).
- `Id` ve `CreatedAt` istemciden gelir. `CreatedAt`, sunucu saatinden 5
  dakikadan fazla ileride olamaz.
- Görev başlığı en fazla 200 karakter (TODO: sınır netleşmedi, varsayım).
- Görev yalnızca atandığı günün KENDİSİNDE tamamlanabilir. Gün, `CompletedAt`
  değerinin yerel tarihine göre belirlenir: istemci `CompletedAt`'i kendi
  yerel offset'iyle gönderir, UTC'ye çevirmez.
- Gelecekteki güne atanmış görev tamamlanamaz; kullanıcı bugüne çekerse
  (`AssignToDate`) o gün tamamlanabilir.
- Geçmiş güne atanmış görev tamamlanamaz. Görev geçmiş günde tamamlanmamış
  olarak KALIR (başarısızlık görünür kalsın diye taşınmaz). Kullanıcı görevi
  yeni bir güne KOPYALAYABİLİR (`CopyToDate`): yeni Id; başlık, öncelik ve odak rengi
  kopyalanır, hatırlatıcı kopyalanmaz; kaynak görev değişmez. Sadece tamamlanmamış görev
  kopyalanabilir.
- Undo: görevin atandığı günün streak değerlendirmesi yapılana kadar mümkündür
  (`Streak.EnsureDayNotEvaluated`). Streak kaydı yoksa geri alınabilir.
- `CompleteTask` ve `UndoTaskCompletion` handler seviyesinde idempotent'tir:
  görev zaten tamamlanmışsa Complete, tamamlanmamışsa Undo hata vermeden döner
  (offline kuyruğun tekrar denemesi için). `CopyTaskToDate` de aynı yeni Id
  ile tekrar gelirse mevcut kopyanın Id'sini döner.
- Gün içi sıralama: her görev `SortOrder` (double) taşır. Sürükle-bırakta
  komşu iki görevin ortası alınır; böylece tek görevi taşımak diğer satırları
  değiştirmez (offline çakışmayı azaltır). Sıralama havuzda ve her günün
  listesinde ayrı geçerlidir. Tamamlanmış görev yeniden sıralanamaz.
  Tamamlananların alta düşmesi görsel sıralamadır, SortOrder'ı değiştirmez.
- Odak rengi: görev opsiyonel olarak tek bir `FocusColor` taşır (sabit 5 renk).
- Silme soft delete'tir (`IsDeleted`, `DeletedAt`). Havuzdaki görev her zaman
  silinebilir; bir güne atanmış görev, günün değerlendirmesi yapılmamışsa
  silinebilir (`Streak.EnsureDayNotEvaluated`). Silinmiş görev değiştirilemez,
  tamamlanamaz, kopyalanamaz. Silme idempotent'tir. Tamamlanmış görev de aynı kurala göre silinebilir.
- Atama ve kopyalama hedefi, kullanıcının yerel "bugün"ü veya sonrası olmalıdır.
  (Kural Application'da, kullanıcının saat dilimiyle kontrol edilir.)
- `SortOrder` değerini istemci hesaplar ve yerleştirme Command'larıyla
  gönderir (oluşturma, güne atama, havuza geri alma, kopyalama, yeniden
  sıralama). Domain yalnızca değerin geçerli (sonlu) bir sayı olduğunu doğrular.
  Eşit değerlerde sıra `CreatedAt`, sonra `Id` ile belirlenir (cihazlar arası
  tutarlılık için).
- Tamamlama ve geri alma `SortOrder`'ı DEĞİŞTİRMEZ. Tamamlananların listede
  alta düşmesi istemcide görsel bir işlemdir; tamamlama tiki kaldırılınca
  görev kendi `SortOrder` değerine göre eski sırasına döner.

## Henüz Kararlaştırılmadı (TODO)
- [x] Undo penceresi: sınırsız mı, süre sınırlı mı (örn. 5 dk)?
- [ ] "Akıllı zaman/öncelik algılama": hangi kelime ve kalıpları tanıyacak,
  istemcide mi sunucuda mı çalışacak?
- [x] Bir görev aynı anda birden fazla FocusArea (renk etiketi) alabilir mi?
- [x] ReminderTime eklendi — AssignedDate'ten bağımsız, opsiyonel TimeOnly
- [x] Kopyalamada hedef gün geçmişte olabilir mi, yalnızca geçmiş günlerin
  görevleri mi kopyalanabilir? (Kullanıcının "bugünü" saat dilimine bağlı;
  Application tasarımında netleşecek.)
- [ ] SortOrder'da ondalık hassasiyet tükenirse yeniden dizme (rebalance) stratejisi.
- [ ] Offline'da dün yapılıp bugün senkronize olan atamaların "geçmiş gün"
  kuralıyla çakışması (istemcinin yerel tarihini mi esas alacağız?).
- [ ] FocusColor'lara kullanıcı tarafından isim verilmesi (v2.0).
- [ ] Silinmiş kayıtların (mezar taşı) kalıcı temizlenme süresi.
- [ ] `CreateTaskCommand` odak rengi almıyor; renk `SetTaskFocusColor`
  komutuyla ayrı atanır. Offline'da iki komut yerine tek komut gerekirse
  birleştirilecek.
- [x] Repository `GetByIdAsync` silinmiş kayıtları DÖNDÜRMEZ (başkasının/silinmiş
  kayıt "bulunamadı" davranışı). Idempotent `DeleteTask` için silinmiş kaydı da
  görebilen ayrı bir sorgu gerekecek; DeleteTask yazılırken netleştirilecek. Çözüm: GetByIdIncludingDeletedAsync, bkz. Command Kataloğu.
- [ ] Sıralamada son eşitlik bozucu `Id` (Guid): sunucu ve istemci aynı
  karşılaştırmayı kullanmalı, yoksa nadir eşitlik durumunda iki cihazda sıra
  farklı çıkar. Mobil aşamasında netleştirilecek (örn. küçük harfli string
  üzerinde ordinal karşılaştırma).

## İlgili Katmanlar
- Domain: `PlanningTask` entity (bkz. Tasks/PlanningTask.cs — referans implementasyon),
  `TaskPriority` ve `FocusColor` (Tasks/)
- Application: tüm görev Command'ları ve Query'leri yazıldı (GetDailyPlan altın
  örnek, GetPool, GetWeeklyPlan); gün değerlendirmesi henüz yok
- Persistence: (henüz yok)
- WebApi: (henüz yok)

## Command Kataloğu (Application)

Ortak kurallar (aşağıdaki tüm Command'lar için):
- Kullanıcı kimliği `ICurrentUserService`'ten alınır, Command'da UserId yoktur.
- Görev `IPlanningTaskRepository.GetByIdAsync(userId, taskId)` ile yüklenir
  (silinmemiş kayıtlar). Bulunamazsa `NotFoundException`; başka kullanıcının
  görevi ve silinmiş görev de "bulunamadı" sayılır.
- Idempotency: hedef durum zaten sağlanmışsa hata verilmez, `SaveChanges`
  çağrılmaz, streak kontrolüne gidilmez. Bu kontrol gün kilidinden ÖNCE yapılır.
- Gün kilidi: görevin mevcut `AssignedDate`'i değerlendirilmiş bir günse
  (`Streak.EnsureDayNotEvaluated`; streak kaydı yoksa kilit yok) görevi
  değiştiren hiçbir Command çalışmaz. İstisna: `CopyTaskToDate` (kaynağı değiştirmez).
- Başlık en fazla 200 karakter, boş olamaz. İstemci zaman damgaları sunucu
  saatinden 5 dakikadan fazla ileride olamaz. `SortOrder` sonlu bir sayı olmalı.
  Enum alanlar tanımlı değerlerden biri olmalı.
- Hedef gün gerektiren Command'larda (AssignTaskToDate, CopyTaskToDate): hedef gün
  kullanıcının yerel "bugün"ü veya sonrasıdır
  (`UserProfile.GetLocalDate(TimeProvider.GetUtcNow())`; `UserProfile` yoksa
  `NotFoundException`), hedef gün değerlendirilmiş olamaz, ve hedef günün
  haftası yoksa oluşturulur. Hafta başlangıcı (Pazartesi) tek bir yardımcıda
  hesaplanır (`Common/Helpers/WeekCalendar.GetWeekStart(DateOnly)`); hafta
  ID'sini handler üretir (`Guid.NewGuid()`). Hedef gün yerel bugünden önceyse `ValidationException` fırlatılır
  (alan adı `TargetDate`/`Date`); hedef gün değerlendirilmişse Domain'in
  `DomainException`'ı olduğu gibi akar.

| Command | Girdi | Kurallar / yan etkiler | Idempotent sayılan durum |
|---|---|---|---|
| UndoTaskCompletion | TaskId | `Streak.EnsureDayNotEvaluated(AssignedDate)`, sonra `Uncomplete()` | görev zaten tamamlanmamış |
| CopyTaskToDate | NewId, SourceTaskId, TargetDate, SortOrder, CreatedAt (döner: NewId) | kaynak yüklenir; `CopyToDate` ile yeni görev eklenir; hedef gün/hafta kuralları | NewId ile görev zaten var (silinmiş olsa da): NewId döner |
| AssignTaskToDate | TaskId, Date, SortOrder | `AssignToDate`; hedef gün/hafta kuralları | AssignedDate == Date ve SortOrder aynı |
| ReturnTaskToPool | TaskId, SortOrder | `ReturnToPool` | zaten havuzda ve SortOrder aynı |
| ReorderTask | TaskId, SortOrder | `Reorder` | SortOrder aynı |
| RenameTask | TaskId, Title | `Rename` | kırpılmış başlık aynı |
| ChangeTaskPriority | TaskId, Priority | `ChangePriority` | öncelik aynı |
| SetTaskFocusColor | TaskId, FocusColor? | `SetFocusColor` (null = rengi kaldır) | renk aynı |
| SetTaskReminder | TaskId, ReminderTime | `SetReminder` | saat aynı |
| ClearTaskReminder | TaskId | `ClearReminder` | hatırlatıcı zaten yok |
| DeleteTask | TaskId, DeletedAt | `GetByIdIncludingDeletedAsync` ile yüklenir (bulunamazsa NotFound); `Delete(DeletedAt)`; DeletedAt gelecekte olamaz | görev zaten silinmiş |

Yeni/değişen repository sözleşmeleri (Application, `Common/Interfaces/Repositories/`):
- `IPlanningTaskRepository`: `GetByIdIncludingDeletedAsync(userId, id, ct)` eklenir
  (silinmiş kayıtları da döner; sadece idempotency kontrolleri ve DeleteTask kullanır).
- `IPlanningWeekRepository` (yeni): `GetByStartDateAsync(userId, weekStartDate, ct)`, `AddAsync(week, ct)`.
- `IUserProfileRepository` (yeni): `GetByIdAsync(userId, ct)`.

Değişiklik: `CreateTaskCommandHandler` idempotency kontrolünü
`GetByIdIncludingDeletedAsync` ile yapar (oluşturulup silinmiş görevin tekrar
gelen "oluştur" isteği birincil anahtar çakışması yaratmasın).

### Henüz Kararlaştırılmadı (TODO)
- [ ] İki cihaz aynı haftayı aynı anda oluşturursa: (UserId, WeekStartDate)
  unique index çakışması Persistence'ta nasıl yönetilecek (mevcut kaydı kullanma)?
- [ ] Gün kilidi `Rename`, `ChangeTaskPriority` gibi küçük değişiklikleri de
  kapsıyor (basitlik için). Gevşetilip gevşetilmeyeceği ürün kararıyla netleşir.

## Query Kataloğu (Application)

Ortak kurallar (tüm Query'ler için):
- Query'ler durum DEĞİŞTİRMEZ: `IUnitOfWork` kullanmaz, `SaveChanges` çağırmaz,
  entity metotlarını çağırmaz. Entity'leri DTO'ya çevirip döner; entity dışarı sızmaz.
- Kullanıcı kimliği `ICurrentUserService`'ten alınır. Repository `userId` ile
  filtreler ve silinmiş görevleri (`IsDeleted`) döndürmez. Başka kullanıcının
  verisi boş sonuç gibi davranır, hata verilmez.
- Sıralama repository'de değil handler'da yapılır (birim testle doğrulanabilsin):
  `SortOrder`, sonra `CreatedAt`, sonra `Id` (hepsi artan).
- Ortak görev DTO'su `TaskDto` (Features/Tasks/Dtos/): Id, Title, Priority,
  FocusColor?, SortOrder, IsCompleted, AssignedDate?, ReminderTime?, CreatedAt,
  CompletedAt?. Tamamlananları alta almak istemcinin işidir, sunucu sıralamayı bozmaz.
- Günün ağırlıklı tamamlanma yüzdesi tek yerde hesaplanır:
  `Common/Helpers/WeightedCompletion.Calculate(IEnumerable<PlanningTask>)`
  → `decimal?`. Kural (Gamification.md ile aynı): ağırlık `(int)Priority`
  (1/2/3); yüzde = tamamlananların ağırlık toplamı / tüm görevlerin ağırlık
  toplamı × 100, 2 ondalığa yuvarlanır (`MidpointRounding.AwayFromZero`).
  Görev yoksa `null` döner (0 değil). Gün değerlendirmesi (EvaluateUserDay)
  da aynı yardımcıyı kullanır, iki yerde farklı sonuç çıkmamalı.
- Gün kilidi: `IsLocked`, kullanıcının streak kaydına göre o gün değerlendirilmişse
  true; streak kaydı yoksa false (`Streak.IsDayEvaluated(date)`).

| Query | Girdi | Dönen | Notlar |
|---|---|---|---|
| GetDailyPlan | Date | `DailyPlanDto(Date, IsLocked, WeightedCompletionPercentage?, Tasks)` | O güne atanmış (`AssignedDate == Date`) silinmemiş görevler; geçmiş günler de sorgulanabilir (arşiv görünümü). Boş gün: boş liste, yüzde null |
| GetPool | — | `IReadOnlyList<TaskDto>` | Havuzdaki (`AssignedDate == null`) silinmemiş görevler |
| GetWeeklyPlan | Date (haftanın herhangi bir günü) | 7 günlük `DailyPlanDto` listesi | Hafta Pazartesi başlar (`WeekCalendar.GetWeekStart`). Görevler `GetByDateRangeAsync` ile TEK sorguda çekilir, handler güne göre gruplar; her gün için `IsLocked` ve yüzde ayrı hesaplanır. Görevi olmayan gün boş liste ve null yüzdeyle döner (7 gün her zaman vardır) |

Repository sözleşmeleri:
- `IPlanningTaskRepository.GetByDateAsync(userId, date, ct)` (yazıldı),
  `GetPoolAsync(userId, ct)` ve `GetByDateRangeAsync(userId, from, to, ct)`
  (hepsi silinmemiş görevleri, sıralamasız döner; `to` dahildir).