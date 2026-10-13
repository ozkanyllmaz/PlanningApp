# Persistence Katmanı Kuralları

## Kapsam (SADECE bunlar)
- EF Core `DbContext` ve entity konfigürasyonları (`IEntityTypeConfiguration`).
- PostgreSQL bağlantı ayarları (Npgsql).
- Migration'lar (`dotnet ef migrations add ...`).
- Application katmanında tanımlanan `IRepository`/`IUnitOfWork`
  interface'lerinin SOMUT implementasyonları.
- Offline-sync için gereken alanlar: her tabloda `LastModifiedAt`,
  `IsSynced`, çakışma (conflict) çözümü için gerekli versiyon/timestamp
  kolonları.

## Yasaklar
- İş kuralı YAZILMAZ ("streak nasıl hesaplanır" burada karar VERİLMEZ,
  sadece Application'ın verdiği kararı kalıcı hale getirir).
- Hosted Service, bildirim tetikleme, e-posta/push gibi dış servis
  kodu YASAK — bunlar Infrastructure'ın işidir.
- Controller veya MediatR referansı YASAK.

## Offline-First'e Özel Kurallar
- Kilit ekranından veya bağlantısızken eklenen kayıtlar yerel/istemci
  tarafında zaten yazılmıştır (mobil SQLite); bu proje sadece sunucu
  tarafı senkronizasyonu karşılar — client-side kod bu projede OLMAZ.
- Senkronizasyon sırasında "son yazan kazanır" mı yoksa alan bazlı
  merge mi uygulanacağı, DbContext seviyesinde net ve tek bir stratejiyle
  (yorum satırıyla açıklanarak) uygulanır; belirsiz/karma strateji
  kullanılmaz.

## Test Beklentisi
- Repository implementasyonları, PlanningApp.IntegrationTests'te
  gerçek/in-memory PostgreSQL'e karşı test edilir (PlanningApp.Test'e
  DEĞİL — o mock kullanır).
- `GetByIdAsync(userId, id)` başka kullanıcının kaydını döndürmez; silinmiş
  kaydı (`IsDeleted`) döndürmez. `GetByIdIncludingDeletedAsync` silinmiş kaydı
  döndürür ama yine başka kullanıcınınkini döndürmez. Bu üç davranış
  IntegrationTests'te gerçek veritabanına karşı doğrulanır.