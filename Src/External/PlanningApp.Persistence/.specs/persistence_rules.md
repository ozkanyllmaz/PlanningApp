# Persistence Katmanı Kuralları

## Kapsam (SADECE bunlar)
- EF Core `PlanningDbContext` ve entity konfigürasyonları (`IEntityTypeConfiguration<T>`).
- PostgreSQL bağlantısı (Npgsql) ve migration'lar.
- Application'da tanımlı repository interface'lerinin ve `IUnitOfWork`'ün
  somut implementasyonları (`PlanningDbContext`, `IUnitOfWork`'ü uygular).
- `AddPersistence(IConfiguration)` DI kaydı (`DependencyInjection.cs`);
  bağlantı dizesi yapılandırmadan okunur, kodda sabit değer YOK.
- Design-time DbContext factory (migration üretimi için).

## Yasaklar
- İş kuralı YAZILMAZ; sadece Application'ın kararını kalıcı hale getirir.
- Hosted Service, bildirim, e-posta/push gibi dış servis kodu YASAK
  (Infrastructure'ın işi).
- Controller veya MediatR referansı YASAK.
- Domain entity'lerine ORM'e özgü attribute veya taviz (parametresiz
  constructor, public setter vb.) istenmez; tüm eşleme konfigürasyon
  sınıflarında yapılır.
- Repository'ler `SaveChanges` ÇAĞIRMAZ; kaydetme Application handler'ındaki
  tek `IUnitOfWork.SaveChangesAsync` çağrısıyla yapılır.

## Eşleme Kuralları
- Her entity için ayrı konfigürasyon sınıfı (`Configurations/` altında),
  `PlanningDbContext` bunları `ApplyConfigurationsFromAssembly` ile yükler.
- Tablo adları açıkça yazılır, çoğul snake_case (`planning_tasks`,
  `planning_weeks`, `streaks`, `user_profiles`). Kolon ve index adları
  snake_case'tir; bu, ek paket olmadan DbContext içindeki küçük bir
  yardımcıyla uygulanır.
- `Id`: `ValueGeneratedNever()` (ID'yi veritabanı üretmez, bkz. entity_rules.md).
- Entity'ler private parametreli constructor ile oluşturulur; EF Core bu
  constructor'ı kullanır (constructor binding). Domain'e parametresiz
  constructor EKLENMEZ.
- `DateTimeOffset` ve `DateTimeOffset?` tüm property'leri `timestamptz` olur.
  Npgsql yalnızca UTC (offset 0) kabul ettiğinden, `ConfigureConventions`
  içinde bir value converter ile yazarken UTC'ye çevrilir. Offset bilgisi
  SAKLANMAZ; okunan değerin offset'i 0'dır. (Domain offset'e yalnızca
  tamamlama anındaki gün doğrulaması için ihtiyaç duyar; gün bilgisi
  `AssignedDate`'te saklıdır.)
- `DateOnly` → `date`, `TimeOnly` → `time`. Enum'lar int olarak saklanır
  (`TaskPriority` ve `FocusColor` değerleri sabittir, değiştirilmez).
  `decimal CompletionPercentage` → `numeric(5,2)`. `Title` en fazla 200,
  `TimeZoneId` en fazla 64 karakter.

## Index'ler ve Benzersizlik
- `planning_tasks`: (user_id, assigned_date) index'i, `is_deleted = false`
  filtreli. Havuz sorgusu için (user_id) index'i, aynı filtreyle.
- `planning_weeks`: (user_id, week_start_date) UNIQUE.
- `streaks`: (user_id) UNIQUE.
- `user_profiles`: birincil anahtar `id`.

## Repository Kuralları
- Her sorgu `UserId` ile filtrelenir. `GetByIdAsync` silinmemiş kaydı,
  `GetByIdIncludingDeletedAsync` silinmiş dahil kaydı döner; ikisi de
  başka kullanıcının kaydını DÖNDÜRMEZ. Silinmişlik filtresi repository'de
  açıkça yazılır, global query filter KULLANILMAZ.
- Tek kayıt getiren metotlar takipli (tracked) entity döner (komutlar
  değiştirip kaydeder). Liste döndüren okuma metotları `AsNoTracking` kullanır.
- Sıralama repository'de YAPILMAZ (handler'da yapılır, bkz. cqrs_rules.md).
- `GetByDateRangeAsync` aralığında `to` dahildir.

## Offline-First'e Özel Kurallar
- Kilit ekranından veya bağlantısızken eklenen kayıtlar istemcide (mobil
  SQLite) tutulur; client-side kod bu projede OLMAZ.
- Senkron için gerekli sürüm/imleç alanları (güncellenme zamanı, sürüm
  numarası) OfflineSync.md'de karar verilene kadar EKLENMEZ.
- `IsSynced` istemci kavramıdır, sunucu tablolarında BULUNMAZ.

## Migration
- Migration'lar `Migrations/` altındadır. `dotnet ef` komutlarında hem proje
  hem başlangıç projesi Persistence'tır (`IDesignTimeDbContextFactory`);
  WebApi'ye bağımlılık olmaz.
- Design-time factory'deki bağlantı dizesi gerçek bir sır içermez
  (migration üretimi veritabanına bağlanmaz).
- Uygulanmış bir migration DEĞİŞTİRİLMEZ; değişiklik yeni migration ile yapılır.

## Test Beklentisi
- Repository ve eşleme davranışları `PlanningApp.IntegrationTests`'te gerçek
  PostgreSQL'e karşı test edilir (PlanningApp.Test'e DEĞİL, o mock kullanır).
- `GetByIdAsync` başka kullanıcının ve silinmiş kaydı döndürmez;
  `GetByIdIncludingDeletedAsync` silinmiş kaydı döndürür ama başka
  kullanıcınınkini döndürmez.
- Tüm property'ler kaydedilip yeniden okunduğunda aynı değerlere sahiptir
  (private constructor ile yeniden oluşturma dahil). +03:00 offset'li bir
  `DateTimeOffset` aynı anı (instant) korur.
- Benzersizlik index'leri ihlalde hata verir.