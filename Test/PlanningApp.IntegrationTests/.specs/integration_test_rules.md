# Entegrasyon Test Kuralları (PlanningApp.IntegrationTests)

- Referanslar: PlanningApp.Application, PlanningApp.Persistence,
  PlanningApp.Infrastructure. WebApi'ye referans YOK.
- Framework: SADECE xUnit; assertion FluentAssertions 7.2.0 (sürüm sabit).
- Veritabanı: gerçek PostgreSQL, Testcontainers ile (Docker çalışıyor olmalı).
  EF InMemory, SQLite ve mock veritabanı KULLANILMAZ; davranış farkları
  testleri yanıltıcı yapar. İmaj: `postgres:17`.
- Tek paylaşılan container (collection fixture). Şema, `Database.MigrateAsync()`
  ile migration'lardan kurulur; `EnsureCreated` KULLANILMAZ (migration'ların
  kendisi de test edilir).
- İzolasyon: her test kendi benzersiz `UserId`'sini (`Guid.NewGuid()`) kullanır;
  testler arasında veri temizleme yoktur. Her test kendi verisini kurar.
- Yazma ve okuma ayrı `DbContext` örnekleriyle yapılır (tracking etkisini
  dışlamak için).
- Klasör yapısı Persistence'ı yansıtır (`Persistence/Repositories/...`).
  İsimlendirme, AAA ve İngilizce metot adları Test projesindeki gibidir.