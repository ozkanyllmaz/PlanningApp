# Application Katmanı Kuralları

## Yapı
- Her use case bir Command (durum değiştirir) veya Query (sadece okur) +
  karşılık gelen Handler'dır (MediatR).
- Klasörleme: Persistence/Infrastructure'ın uygulayacağı TÜM interface'ler
  (`IPlanningTaskRepository`, `IUnitOfWork`, `ICurrentUserService` vb.)
  `Common/Interfaces/` altında tutulur (repository'ler `Repositories/` alt
  klasöründe). Use case'ler feature bazlı `Features/` altında, her biri kendi
  klasöründe: `Features/Tasks/Commands/CreateTask/` içinde
  `CreateTaskCommand.cs`, `CreateTaskCommandHandler.cs`,
  `CreateTaskCommandValidator.cs`. Query'ler için `Features/Tasks/Queries/GetWeeklyPlan/` gibi (hafta kaydının kendisini ilgilendiren sorgular, örn. arşiv, `Features/Weeks/Queries/` altındadır). Ortak altyapı
  (`Behaviors/`, `Exceptions/`) `Common/` altındadır.
- İsimlendirme: `CreateTaskCommand`, `AssignTaskToDateCommand`,
  `CompleteTaskCommand`, `UndoTaskCompletionCommand`, `RenameTaskCommand`,
  `GetWeeklyPlanQuery`, `GetDailyPlanQuery`, `GetArchivedWeeksQuery`.
- Her Command/Query için bir `Validator` (FluentValidation) yazılır ve
  MediatR pipeline behavior'ı üzerinden otomatik tetiklenir. Handler içine
  manuel validation kodu YAZILMAZ.
- Query'ler durum değiştirmez (`IUnitOfWork` yok), entity döndürmez, DTO döner.
  DTO'lar kendi feature klasöründe `Dtos/` altında, Query'ye özel olanlar
  Query klasörünün içindedir. Ayrıntılar: docs/specs/TaskManagement.md → Query Kataloğu.
  Altın örnek: `GetDailyPlanQueryHandler`.

## Command Handler Akış Sırası
Var olan bir kaydı değiştiren Command handler'ları şu sırayı izler
(altın örnek: `CompleteTaskCommandHandler`):
1. Kaydı repository'den yükle; bulunamazsa `NotFoundException`.
2. Idempotency: hedef durum zaten sağlanmışsa hiçbir şeye gitmeden dön.
3. Gün kilidi: `Streak.EnsureDayNotEvaluated` (streak kaydı yoksa kilit yok).
4. Domain metodunu çağır.
5. `SaveChanges` bir kez.
Hedef gün gerektirenlerde kullanıcının yerel bugünü hesaplanır; hedef gün
yerel bugünden önceyse `ValidationException` (alan adı `Date`/`TargetDate`).

## Ortak Yardımcılar (kopyalanmaz, tek yerden kullanılır)
- İstemci zaman damgası kuralı: `Common/Validation/ValidatorExtensions.MustNotBeInFuture`.
- Ağırlıklı tamamlanma yüzdesi: yalnızca `Common/Helpers/WeightedCompletion`.
- Hafta başlangıcı: yalnızca `Common/Helpers/WeekCalendar.GetWeekStart`.

## Kimlik (ID) ve Zaman
- İstemcinin (mobil, offline) oluşturduğu kayıtlar için ID Command'ın
  içinde istemciden gelir (örn. `CreateTaskCommand.Id`). Handler bu ID'yi
  kullanır, kendisi `Guid.NewGuid()` üretmez. Sunucunun kendi oluşturduğu
  kayıtlarda (örn. `PlanningWeek`) ID'yi handler üretir.
- Zaman için .NET'in `TimeProvider` sınıfı constructor ile enjekte edilir.
  `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.UtcNow` doğrudan
  kullanılmaz. Alınan zaman Domain metotlarına parametre olarak geçirilir.
- İstemciden gelen ID ile oluşturma Command'ları idempotent yazılır: aynı
  kullanıcı için aynı ID varsa hata fırlatılmaz, mevcut kaydın ID'si döner. Complete ve Undo gibi durum değiştiren Command’lar da hedef durum zaten sağlanmışsa hata vermeden döner.
  Altın örnek: `CreateTaskCommandHandler`.

## Kullanıcı Kimliği ve Sahiplik
- `UserId` Command/Query'nin body'sinden ALINMAZ (istemciye güvenilmez).
  Application'da tanımlı `ICurrentUserService` interface'i üzerinden alınır,
  somut implementasyonu (JWT'den okuma) WebApi'dedir.
- Repository sorguları her zaman `UserId` ile filtrelenir. Başka bir
  kullanıcıya ait kayıt, "bulunamadı" olarak davranılır (kaydın var
  olduğu bilgisi sızdırılmaz).

## Sorumluluk Sınırı
- Bu katman `IPlanningTaskRepository`, `IPlanningWeekRepository`,
  `IStreakRepository`, `IUnitOfWork` gibi interface'leri TANIMLAR;
  implementasyonu Persistence'a bırakır (Dependency Inversion).
- Bildirim gönderme, hosted service tetikleme gibi işler için de interface
  tanımlanır (örn. `INotificationTrigger`); somut kod Infrastructure'dadır.
- İş kuralı burada yaşır, Persistence/Infrastructure'da DEĞİL:
- Günün ağırlıklı tamamlanma yüzdesini hesaplamak (kullanıcının o güne
    atanmış görevlerini çekip `TaskPriority` ağırlıklarıyla) ve sonucu
    `Streak.EvaluateDay()` metoduna vermek.
- "Akıllı zaman/öncelik algılama" (metinden parse etme).
- Gün kilidi ve undo kuralı: gün değerlendirilmişse görev değiştirilemez
    (`Streak.EnsureDayNotEvaluated`), bkz. TaskManagement.md.
- Entity'nin kendi durumunu koruyan kurallar Domain'de kalır, burada
  tekrar yazılmaz.
- Controller'ların ihtiyaç duyduğu her response şekli burada DTO/Response
  modeli olarak tanımlanır; WebApi kendi mapping'ini yapmaz.

## Exception'lar
- Bu katmanın kendi exception tipleri (`ValidationException`,
  `NotFoundException`) SADECE bu projede tanımlanır. Domain'den gelen
  `DomainException` olduğu gibi yukarı akar, burada sarmalanmaz.

## Yasaklar
- EF Core, Npgsql gibi somut veritabanı paketleri YASAK.
- HTTP/ASP.NET Core'a özgü tipler (`HttpContext`, `[ApiController]` vb.) YASAK.
- Handler içinde doğrudan `DbContext` kullanımı YASAK (sadece repository
  interface'leri).

## Test Beklentisi
- Her Handler için: bir happy-path testi + bir validation-failure testi
  (PlanningApp.Test, repository'ler Moq ile mock'lanarak, zaman için
  `FakeTimeProvider`). Validation testleri Validator sınıfı için ayrı yazılır.
- Handler, `ICurrentUserService.UserId`'yi repository çağrılarına geçirir;
  bu, ilgili handler testinde doğrulanır.
- Var olan bir kaydı değiştiren Command handler'larında ek olarak: kayıt
  bulunamazsa (başka kullanıcının kaydı da repository'den `null` döner)
  `NotFoundException` fırlatılır ve `SaveChanges` çağrılmaz; idempotent
  durumda hata yoktur ve `SaveChanges` çağrılmaz.
- Query handler'ları için: sıralamanın ve DTO eşlemesinin doğru olduğu test
  edilir. Başka kullanıcının verisinin dönmemesi Persistence integration
  testinde doğrulanır.