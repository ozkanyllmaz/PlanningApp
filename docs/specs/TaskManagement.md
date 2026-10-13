# Task Management — Özellik Spesifikasyonu

## Durum: Taslak (Domain tasarımı sürüyor)

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
- Tamamlama geri alınabilir (`Uncomplete()` / undo) — ne kadar süre içinde
  geri alınabileceği HENÜZ KARARLAŞTIRILMADI (aşağıya bkz.).
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
  yeni bir güne KOPYALAYABİLİR (`CopyToDate`): yeni Id, aynı başlık ve öncelik,
  hatırlatıcı kopyalanmaz, kaynak görev değişmez. Sadece tamamlanmamış görev
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
  tamamlanamaz, kopyalanamaz. Silme idempotent'tir.
- Atama ve kopyalama hedefi, kullanıcının yerel "bugün"ü veya sonrası olmalıdır.
  (Kural Application'da, kullanıcının saat dilimiyle kontrol edilir.)
- `SortOrder` değerini istemci hesaplar ve yerleştirme Command'larıyla
  gönderir (oluşturma, güne atama, havuza geri alma, kopyalama, yeniden
  sıralama). Domain yalnızca değerin geçerli (sonlu) bir sayı olduğunu doğrular.
  Eşit değerlerde sıra `CreatedAt`, sonra `Id` ile belirlenir (cihazlar arası
  tutarlılık için).
- Kopyalama (`CopyToDate`) başlık, öncelik ve odak rengini taşır;
  hatırlatıcıyı taşımaz.
- Silinmiş görev hiçbir şekilde değiştirilemez, tamamlanamaz, kopyalanamaz.
  Tamamlanmış bir görev silinebilir (günü değerlendirilmediyse).

## Henüz Kararlaştırılmadı (TODO)
- [x] Undo penceresi: sınırsız mı, süre sınırlı mı (örn. 5 dk)?
- [ ] "Akıllı zaman/öncelik algılama" hangi kelimeleri/pattern'leri tanıyacak?
- [x] Bir görev aynı anda birden fazla FocusArea (renk etiketi) alabilir mi?
- [x] ReminderTime eklendi — AssignedDate'ten bağımsız, opsiyonel TimeOnly
- [x] Kopyalamada hedef gün geçmişte olabilir mi, yalnızca geçmiş günlerin
  görevleri mi kopyalanabilir? (Kullanıcının "bugünü" saat dilimine bağlı;
  Application tasarımında netleşecek.)
- [ ] SortOrder'da ondalık hassasiyet tükenirse yeniden dizme (rebalance) stratejisi.
- [ ] Offline'da dün yapılıp bugün senkronize olan atamaların "geçmiş gün"
  kuralıyla çakışması (istemcinin yerel tarihini mi esas alacağız?).
- [ ] "Akıllı zaman/öncelik algılama" istemcide mi sunucuda mı çalışacak?
- [ ] FocusColor'lara kullanıcı tarafından isim verilmesi (v2.0).
- [ ] Silinmiş kayıtların (mezar taşı) kalıcı temizlenme süresi.
- [ ] `CreateTaskCommand` odak rengi almıyor; renk `SetTaskFocusColor`
  komutuyla ayrı atanır. Offline'da iki komut yerine tek komut gerekirse
  birleştirilecek.
- [ ] Repository `GetByIdAsync` silinmiş kayıtları DÖNDÜRMEZ (başkasının/silinmiş
  kayıt "bulunamadı" davranışı). Idempotent `DeleteTask` için silinmiş kaydı da
  görebilen ayrı bir sorgu gerekecek; DeleteTask yazılırken netleştirilecek.

## İlgili Katmanlar
- Domain: `PlanningTask` entity (bkz. Tasks/PlanningTask.cs — referans implementasyon)
- Application: CreateTaskCommand, CompleteTaskCommand, UndoTaskCompletionCommand,
  AssignTaskToDateCommand, ReturnTaskToPoolCommand, RenameTaskCommand (henüz yazılmadı)
- Persistence: (henüz yok)
- WebApi: (henüz yok)