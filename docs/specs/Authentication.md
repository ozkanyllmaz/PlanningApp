# Authentication — Özellik Spesifikasyonu

## Durum: Taslak

## Kesinleşen Kurallar
- Uygulama açılışında giriş/kayıt ekranı vardır; e-posta+şifre ve Google girişi.
- Kullanıcı deposu ASP.NET Core Identity'dir. Identity tipleri SADECE
  Persistence/Infrastructure'da yaşar; Domain'e sızmaz. Domain'de `UserProfile`
  (Id = Identity kullanıcı Id'si, IANA saat dilimi) bulunur.
- Oturum: kısa ömürlü JWT access token + refresh token. Token üretimi ve
  Google doğrulaması Infrastructure'da, Application'da tanımlı arayüzlerin
  arkasındadır.
- Google girişi: mobil Google'dan kimlik token'ı alır, API doğrular, kullanıcıyı
  bulur/oluşturur, kendi token'larımızı döner.
- `UserProfile` (Domain/Users) kayıt sırasında oluşturulur: Id = Identity
  kullanıcı Id'si, `TimeZoneId` (IANA). Saat dilimi geçerli olmalıdır.
- "Yerel gün" hesabı `UserProfile.GetLocalDate(instant)` ile yapılır.

## Henüz Kararlaştırılmadı (TODO)
- [ ] E-posta doğrulama ve şifre sıfırlama (e-posta gönderen servis gerekir).
- [ ] iOS'ta yayınlanırsa Apple'ın giriş yöntemi kuralları (araştırılacak).
- [ ] Refresh token süresi ve iptal (logout) stratejisi.
- [ ] Saat dilimi nasıl alınacak (cihazdan otomatik, kullanıcı değiştirebilir mi)?
- [ ] Linux/Docker üzerinde saat dilimi veritabanı (tzdata) ve ICU kurulu olmalı; yoksa IANA kimlikleri çözülemez.
- [ ] `IUserProfileRepository`'ye `AddAsync` eklenecek (kayıt akışında profil oluşturma).