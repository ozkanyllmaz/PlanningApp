# API Test Kuralları (PlanningApp.ApiTests)

- Referans: yalnızca PlanningApp.WebApi (diğer projeler geçişli gelir). Framework: xUnit;
  assertion FluentAssertions 7.2.0 (sürüm sabit).
- Gerçek HTTP: `WebApplicationFactory<Program>`; gerçek PostgreSQL: Testcontainers (`postgres:17`),
  tek paylaşılan container (collection fixture). Docker çalışıyor olmalı.
- Ortam adı "Testing". Yapılandırma `UseSetting` ile verilir (bağlantı dizesi, `Jwt:*`); bunun
  gerçekten uygulamaya ulaştığı bir testle kanıtlanır. Ulaşmazsa ortam değişkenleri kullanılır ve raporlanır.
- Şema, fixture'da `Database.MigrateAsync()` ile kurulur. JWT imzalama anahtarı her çalıştırmada
  `RandomNumberGenerator` ile üretilir; repoda sabit anahtar OLMAZ.
- İzolasyon: her test kendi benzersiz e-postasıyla kullanıcı oluşturur; veri temizleme yok.
- Hata yanıtları için içerik türü `application/problem+json` ve durum kodu doğrulanır; kimlik hatası
  testlerinde yanıtın parolayı, token'ı veya hesap varlığına dair ipucunu içermediği doğrulanır.
- Hız sınırı testi, düşük limitli AYRI bir factory ile yapılır (diğer testler etkilenmesin).
- Mimari testler: controller'lar yalnızca `ISender` alır; tüm yollar `api/v1/` ile başlar; `[AllowAnonymous]`
  olan action'lar açıkça listelenmiş beklenen kümeyle birebir aynıdır (yeni anonim uç eklemek testi
  bilinçli güncellemeyi gerektirir).