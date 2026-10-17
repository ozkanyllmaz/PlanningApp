# Authentication — Özellik Spesifikasyonu

## Durum: İlk dilim (e-posta + şifre) tasarlandı; Google girişi, e-posta doğrulama ve şifre sıfırlama ikinci dilimde

## Kesinleşen Kurallar

### Genel
- Kullanıcı deposu ASP.NET Core Identity'dir (rolsüz). Identity tipleri (`ApplicationUser` vb.)
  SADECE Persistence'ta yaşar; Domain ve Application'a sızmaz. Application, Identity'ye yalnızca
  kendi arayüzü `IIdentityService` üzerinden erişir.
- `UserId` Guid'dir ve sunucu tarafından üretilir (kayıt çevrimiçi yapılır, offline kayıt YOK).
  `UserProfile.Id` her zaman Identity kullanıcı Id'sine eşittir.
- Kayıt ve giriş e-posta ile yapılır (e-posta kullanıcı adıdır; benzersiz, büyük/küçük harf duyarsız).
- Parola: en az 8 karakter, karmaşıklık zorunluluğu yok. Kural Identity seçeneklerindedir (tek
  kaynak); Command validator'ı yalnızca boş olmama ve üst sınırı (128) denetler.
- Kilitleme: 5 hatalı denemede 15 dakika (Identity lockout). Kimlik doğrulama hataları (bilinmeyen
  e-posta, yanlış parola, kilitli hesap) AYNI genel mesajı döner: "E-posta veya şifre hatalı."

### Token'lar
- Access token: JWT (HS256), 15 dk. Claim'ler: `sub` (UserId), `email`, `jti`, `iat`, `exp`. Rol veya
  başka yetki claim'i yok.
- Refresh token: 32 bayt kriptografik rastgele, base64url. Veritabanında SADECE SHA-256 özeti
  saklanır; düz token yalnızca üretildiği yanıtta bulunur, hiçbir yerde loglanmaz. Ömür 60 gün. Özet, SHA-256'nın küçük harfli onaltılık (hex) gösterimidir (64 karakter).
- Yenileme (rotation): refresh token kullanılınca eskisi iptal edilir (`ReplacedByTokenId`), yenisi
  verilir. İptal edilmiş, süresi dolmuş veya bilinmeyen token → `AuthenticationFailedException`
  (istemci giriş ister). Yeniden kullanım tespiti (oturum ailesinin iptali) v1'de YOK. Aynı token ile eşzamanlı iki yenileme isteğinden biri DbUpdateConcurrencyException ile reddedilir (5xx); istemci tekrar dener ve iptal edilmiş token ile 401 alır.
- Çıkış: refresh token iptal edilir; token yoksa veya zaten iptalse hata verilmez (idempotent,
  bilgi sızdırılmaz).
- İmzalama anahtarı ve süreler `Jwt` yapılandırma bölümünden gelir. Anahtar kodda/appsettings'te
  YOK (user-secrets veya ortam değişkeni); base64 çözülmüş hali 32 bayttan kısaysa uygulama
  başlangıçta hata verir.

### Domain
- `RefreshToken` entity (Domain/Auth/): `Id`, `UserId`, `TokenHash`, `CreatedAt`, `ExpiresAt`,
  `RevokedAt?`, `ReplacedByTokenId?`. `Create(id, userId, tokenHash, createdAt, expiresAt)`: UserId
  boş olamaz, TokenHash boş olamaz, ExpiresAt > CreatedAt (aksi halde `DomainException`).
  `IsActive(asOf)`: RevokedAt null VE asOf < ExpiresAt. `Revoke(revokedAt, replacedByTokenId?)`:
  zaten iptal edilmişse `DomainException`. Zaman her zaman parametreyle gelir.

### Application: arayüzler (Common/Interfaces/)
- `IIdentityService`:
  - `CreateUserAsync(userId, email, password, ct)` → `IdentityOperationResult(Succeeded, Errors)`;
    `Errors` alan adı → mesajlar sözlüğüdür: e-posta çakışması "Email", parola kuralı ihlali "Password".
  - `CheckCredentialsAsync(email, password, ct)` → `CredentialCheckResult(Succeeded, UserId?, Email?)`;
    hatalı deneme sayacı ve kilitleme burada yönetilir.
  - `FindByIdAsync(userId, ct)` → `AppUserInfo(UserId, Email)?`
  - `DeleteUserAsync(userId, ct)`
- `ITokenService`:
  - `CreateAccessToken(userId, email, issuedAt)` → `AccessTokenResult(Token, ExpiresAt)`
  - `CreateRefreshToken(issuedAt)` → `RefreshTokenMaterial(PlainToken, TokenHash, ExpiresAt)`
  - `HashRefreshToken(plainToken)` → özet
- `IRefreshTokenRepository`: `GetByHashAsync(hash, ct)` (takipli), `AddAsync(token, ct)`.
- `IUserProfileRepository`: `AddAsync(profile, ct)` eklenir.
- `AuthenticationFailedException` (Application/Common/Exceptions/).

### Application: Command'lar (Features/Auth/Commands/)
Hepsi anonimdir (`ICurrentUserService` KULLANILMAZ). Zaman `TimeProvider`'dan gelir. Dönen
`AuthResult(UserId, AccessToken, AccessTokenExpiresAt, RefreshToken, RefreshTokenExpiresAt)`.
Parola ve token alanı taşıyan Command record'ları `ToString`'i override eder, bu alanlar "***" yazılır.

| Command | Girdi | Akış |
|---|---|---|
| Register | Email, Password, TimeZoneId | Validator: e-posta biçimi (en çok 256), parola boş değil ve ≤128, `TimeZoneId` geçerli. UserId üret; `CreateUserAsync` (başarısızsa alan hatalarıyla `ValidationException`, hiçbir şey kaydedilmez); `UserProfile.Create`; refresh token üret ve ekle; TEK `SaveChanges`; SaveChanges hata verirse `DeleteUserAsync` ile telafi et ve hatayı yeniden fırlat; access token üret |
| Login | Email, Password | `CheckCredentialsAsync`; başarısızsa `AuthenticationFailedException` (genel mesaj, token üretilmez, SaveChanges yok); başarılıysa yeni refresh token ekle, `SaveChanges`, access token üret |
| RefreshAccessToken | RefreshToken | Hash'le; `GetByHashAsync`; yoksa veya aktif değilse `AuthenticationFailedException`; `FindByIdAsync` (yoksa aynı hata); eskiyi iptal et (ReplacedByTokenId = yeni Id), yenisini ekle, TEK `SaveChanges`; access token üret |
| Logout | RefreshToken | Hash'le; token yoksa veya zaten iptalse hiçbir şey yapmadan dön; değilse iptal et, `SaveChanges` |

- Register telafisi: kullanıcı oluştuktan sonraki TÜM adımlar (profil ve token oluşturma,
  ekleme, `SaveChanges`) tek bir try bloğundadır; herhangi biri hata verirse `DeleteUserAsync`
  çağrılır (iptal belirteci OLMADAN) ve ilk hata yeniden fırlatılır. Telafinin kendisi hata
  verirse ilk hata MASKELENMEZ (telafi hatası yutulur).
- Login validator: e-posta boş olamaz, biçimi geçerli ve en çok 256 karakter; parola boş olamaz
  ve en çok 128 karakter.
- Hassas alan taşıyan sonuç tipleri (`AuthResult`, `AccessTokenResult`, `RefreshTokenMaterial`)
  `ToString`'de token alanlarını "***" gösterir.

## Henüz Kararlaştırılmadı (TODO)
- [ ] Google girişi (ikinci dilim): Google Cloud projesi, OAuth istemci kimliği, sunucu tarafı
  kimlik token'ı doğrulama.
- [ ] E-posta doğrulama ve şifre sıfırlama (e-posta gönderen servis gerekir). Yayına çıkmadan ÖNCE kapatılmalı.
- [ ] Yenileme token'ı yeniden kullanım tespiti (aile iptali) ve zayıf ağ toleransı.
- [ ] "Tüm cihazlardan çıkış" ve süresi dolmuş token'ların temizliği (Hosted Service).
- [ ] Kayıtta e-posta çakışması hesabın varlığını sızdırır (hesap numaralandırma); e-posta
  doğrulama gelince yeniden değerlendirilecek.
- [ ] Giriş denemelerinde zamanlama farkı (bilinmeyen e-postada parola karması hesaplanmıyor).
- [ ] Kimlik uçlarında hız sınırlama (ASP.NET Core rate limiting), WebApi dilimi.
- [ ] Hesap silme ve kişisel veri dışa aktarma (KVKK), yayına çıkmadan önce.
- [ ] iOS'ta Google ile giriş sunulursa Apple'ın "Apple ile giriş" kuralı (araştırılacak).
- [ ] JWT imzalama anahtarı döndürme (rotation) ve anahtar kimliği (`kid`).
- [ ] Mobilde refresh token güvenli depoda saklanmalı (mobil aşamasında).
- [ ] Kayıt sonrası yarım kalan kullanıcı (profil yok) durumu için onarım yolu.
- [ ] Saat dilimi cihazdan otomatik alınır; kullanıcı sonradan değiştirebilir (ayrı Command).
- [ ] Linux/Docker üzerinde saat dilimi veritabanı (tzdata) ve ICU kurulu olmalı.