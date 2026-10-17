# WebApi Katmanı Kuralları

## Kapsam
- Controller'lar SADECE ilgili MediatR Command/Query'sini `ISender` ile gönderir ve sonucu döner.
  İçlerinde iş mantığı, if/else karar zinciri, repository/DbContext/UserManager kullanımı OLMAZ.
  Controller constructor'ı yalnızca `ISender` alır (ApiTests'teki mimari test doğrular).
- Command/Query'ler HTTP gövdesine veya rotaya doğrudan bağlanır; WebApi'de ek istek/yanıt modeli
  ve gölge mapping YOKTUR. Yanıt gövdesi Application'daki sonuç tipidir. `UserId` hiçbir istekte
  alınmaz (oturumdan gelir).
- Tüm uçlar `/api/v1/...` altındadır; yollar açıkça yazılır (örn. `[Route("api/v1/auth")]`),
  küçük harf, kebab-case. Her action için dönen durum kodları `ProducesResponseType` ile belirtilir.
- `Program.cs` composition root'tur: `AddApplication()`, `AddPersistence(configuration)`,
  `AddInfrastructure(configuration)`, `TimeProvider.System`, `ICurrentUserService`, kimlik doğrulama,
  hata işleme, hız sınırlama, controller'lar. İş mantığı içermez.
- OpenAPI (yerleşik `Microsoft.AspNetCore.OpenApi`) yalnızca Development'ta açılır.
- Model bağlamada "null olamayan referans tipi zorunludur" otomatik kuralı KAPALIDIR
  (`SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`): eksik alan FluentValidation
  tarafından yakalanır, böylece tek bir hata biçimi kalır.

## Kimlik Doğrulama ve Yetkilendirme
- JWT Bearer. `MapInboundClaims = false` (claim adları değişmez; kullanıcı `sub` claim'indedir).
  Doğrulama: imza, issuer, audience, süre; yalnızca HS256; saat sapması 30 sn. İmzalama anahtarı
  `JwtOptions`'tan, Infrastructure'daki TEK yardımcıyla çözülür (`JwtTokenService` ile aynı kod yolu).
- Varsayılan yetkilendirme (fallback policy): kimliği doğrulanmış kullanıcı. Anonim uçlar
  `[AllowAnonymous]` ile AÇIKÇA işaretlenir.
- `ICurrentUserService` gerçeklemesi (`HttpContextCurrentUserService`) buradadır: `sub` claim'ini
  Guid olarak okur; kimliksiz ya da geçersizse `InvalidOperationException` (handler'lara yalnızca
  yetkili istek ulaşır).
- CORS: mobil istemci gerektirmez, web istemcisi (v2.0) gelince eklenir. HTTPS yönlendirme uygulama
  içinde YOK; TLS barındırma ortamında (ters vekil) sonlanır.

## Hata Yanıt Formatı
- `IExceptionHandler` + `AddProblemDetails` + `UseStatusCodePages`: tüm hata yanıtları
  `application/problem+json` (RFC 9457); gövdesiz 401/404/405/429 yanıtlarına da gövde eklenir.
- Eşleme:
  - `ValidationException` (Application) → 400, `errors`: alan → mesajlar (anahtarlar Application'ın
    verdiği adlardır, PascalCase)
  - `NotFoundException` → 404
  - `AuthenticationFailedException` → 401 + `WWW-Authenticate: Bearer`
  - `DomainException` → 422
  - `DbUpdateConcurrencyException` → 503 + `Retry-After: 1` (geçicidir, istemci tekrar dener)
  - diğer her şey → 500
- 500 yanıtı genel bir mesaj taşır; exception mesajı, stack trace, SQL ve kullanıcıya ait veri ASLA
  yazılmaz. Beklenmeyen hatalar Error düzeyinde loglanır. İstek ve yanıt gövdeleri LOGLANMAZ
  (parola ve token içerir).
- Bozuk JSON gövdesi 400'dür. İstemcinin hata sınıflandırması: docs/specs/OfflineSync.md.

## Hız Sınırlama
- Yerleşik rate limiter. "auth" politikası: IP başına sabit pencere (1 dk), limit
  `RateLimiting:Auth:PermitLimit` (varsayılan 20), kuyruk yok. `register`, `login`, `refresh`,
  `logout` uçlarına uygulanır. Aşımda 429 + `Retry-After` + problem+json.

## Yapılandırma ve Gizli Bilgiler
- `appsettings.json`: `Jwt:Issuer`, `Jwt:Audience`, `RateLimiting:Auth:PermitLimit`. Gizli değer YAZILMAZ.
- `Jwt:SigningKey` ve `ConnectionStrings:PlanningDb`: Development'ta user-secrets, diğer ortamlarda
  ortam değişkeni. Eksikse uygulama başlamaz.
- Yalnızca Development'ta açılışta `Database.MigrateAsync()` çalışır (yerel kolaylık). Diğer
  ortamlarda migration dağıtım adımıdır; uygulama kendi şemasını DEĞİŞTİRMEZ.

## Yasaklar
- Repository, DbContext, UserManager, Hosted Service controller'da KULLANILMAZ.
- Hata yanıtlarında başka bir kullanıcının kaydının varlığına dair bilgi sızdırılmaz.
- Parola, token veya JWT anahtarı log'a, hata mesajına, yanıta yazılmaz.

## Test Beklentisi
- Testler `Test/PlanningApp.ApiTests` projesindedir (bkz. oradaki .specs).
- Exception → HTTP durum kodu eşlemesinin her satırı için bir test bulunur.

## Henüz Kararlaştırılmadı (TODO)
- [ ] Ters vekil arkasında gerçek istemci IP'si (forwarded headers) ve operatör NAT'ı arkasındaki
  kullanıcıların aynı IP'yi paylaşması; hız sınırı politikası buna göre gözden geçirilecek.
- [ ] Çok örnekli çalışmada dağıtık hız sınırı sayacı (şimdi bellek içi).
- [ ] Sağlık uçları (`/health`), HSTS, CORS ve dağıtım ayarları (barındırma kararıyla birlikte).
- [ ] Üretimde veritabanı migration'larının nasıl uygulanacağı (dağıtım hattı).
- [ ] `DbUpdateException` (örn. eşzamanlı kayıtta benzersizlik ihlali) için 409/400 eşlemesi.