# Application Katmanı Kuralları

## Yapı
- Her use case bir Command veya Query + karşılık gelen Handler'dır (MediatR).
- İsimlendirme: `CreateTaskCommand`, `MoveTaskToDayCommand`,
  `CompleteTaskCommand`, `UndoTaskCompletionCommand`,
  `GetWeeklyPlanQuery`, `GetDailyPlanQuery`, `GetArchivedWeeksQuery`.
- Her Command/Query için bir `Validator` (FluentValidation) yazılır ve
  MediatR pipeline behavior'ı üzerinden otomatik tetiklenir — Handler
  içine manuel validation kodu YAZILMAZ.

## Sorumluluk Sınırı
- Bu katman, `IRepository`/`IUnitOfWork` gibi interface'leri TANIMLAR;
  implementasyonu Persistence'a bırakır (Dependency Inversion).
- Bildirim gönderme, hosted service tetikleme gibi işler için de
  interface tanımlanır (örn. `INotificationTrigger`); somut kod
  Infrastructure'dadır.
- İş kuralı burada yaşar, Persistence/Infrastructure'da DEĞİL. Örnekler:
  - Streak hesaplama mantığı ("bir gün kaçırılırsa sıfırlanır").
  - "Akıllı zaman/öncelik algılama" (yazılan metinden parse etme) mantığı.
  - Undo penceresi kuralı (görev tamamlama geri alınabilir mi, ne kadar
    süre içinde).
- Controller'ların ihtiyaç duyduğu her response şekli burada DTO/Response
  modeli olarak tanımlanır; WebApi kendi mapping'ini yapmaz.

## Yasaklar
- EF Core, Npgsql gibi somut veritabanı paketleri YASAK.
- HTTP/ASP.NET Core'a özgü tipler (`HttpContext`, `[ApiController]` vb.) YASAK.

## Test Beklentisi
- Her Handler için: bir happy-path testi + bir validation-failure testi
  (PlanningApp.Test, repository'ler mock'lanarak).