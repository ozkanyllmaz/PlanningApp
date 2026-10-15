# Infrastructure Katmanı Kuralları

## Kapsam (SADECE bunlar)
- `.NET Hosted Services`: streak/seri hesaplama arkaplan işi, günlük
  özlü söz yenileme, zamanlı bildirim tetikleme.
- Maskot'un "zamanlı akıllı bildirimler" göndermesini tetikleyen servisler.
- Push notification / e-posta gibi üçüncü parti entegrasyon istemcileri.
- Application katmanında tanımlanan dış-servis interface'lerinin
  (`INotificationTrigger` vb.) somut implementasyonları.
- Kimlik: `JwtTokenService` (`ITokenService`): erişim ve yenileme token'ı üretimi, yenileme
  token'ının SHA-256 özeti. `JwtOptions` (`Jwt` bölümü: Issuer, Audience, SigningKey,
  AccessTokenMinutes = 15, RefreshTokenDays = 60) `ValidateOnStart` ile doğrulanır; SigningKey base64
  çözülmüş en az 32 bayt olmalıdır, aksi halde uygulama BAŞLAMAZ. Anahtar kodda/appsettings'te YOK.
  Rastgelelik `RandomNumberGenerator` ile üretilir (`System.Random` YASAK). Zaman `TimeProvider`'dan.
  Bu servis veritabanına dokunmaz. `AddInfrastructure(IConfiguration)` DI kaydını yapar.

## Yasaklar
- Veri erişimi/DbContext kodu KESİNLİKLE YASAK — bu Persistence'ın işi.
  Infrastructure, veriye ihtiyaç duyarsa Persistence'ın sunduğu
  repository/interface üzerinden (Application aracılığıyla) erişir,
  kendi DbContext'ini asla açmaz.
- İş kuralı YAZILMAZ; sadece Application'ın verdiği kararı dış dünyaya
  (bildirim, arkaplan tetikleme) yansıtır.

## Test Beklentisi
- Hosted Service davranışları PlanningApp.IntegrationTests'te test edilir.
- JwtTokenService davranışları IntegrationTests'te test edilir (veritabanı gerektirmeyen testler de bu projede durur).