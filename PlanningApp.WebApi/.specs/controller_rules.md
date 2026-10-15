# WebApi Katmanı Kuralları

## Kapsam
- Controller'lar SADECE ilgili MediatR Command/Query'sini gönderir ve
  sonucu döner. İçlerinde if/else karar zinciri, iş mantığı OLMAZ.
- Kimlik doğrulama: JWT doğrulama middleware'i, kayıt/giriş/Google girişi/
  token yenileme endpoint'leri (bkz. docs/specs/Authentication.md).
  Endpoint'ler Application'daki Command'lara devreder; token üretimi ve
  Google doğrulaması Infrastructure'dadır.
- `ICurrentUserService`'in somut implementasyonu BURADADIR: JWT içindeki
  kullanıcı kimliğini (claim) okur. Command/Query body'sinden kullanıcı
  kimliği ALINMAZ.
- Uygulama başlangıcında (`Program.cs`) bağımlılık kayıtları yapılır:
  `AddApplication()`, Persistence ve Infrastructure kayıtları ve
  `TimeProvider.System`. Zaman için başka bir kayıt yapılmaz.
- Swagger/OpenAPI konfigürasyonu.
- Global exception handling middleware (aşağıya bkz.).
- Kilit ekranı hızlı erişimin ihtiyaç duyduğu endpoint'ler burada
  tanımlanır; mantık yine Application'daki Command'lara devredilir
  (bkz. docs/specs/LockScreenQuickAccess.md, kesinleşmedi).

## Hata Yanıt Formatı
- Tüm hata yanıtları `ProblemDetails` (RFC 9457) formatındadır.
- Eşleme:
  - `ValidationException` (Application) → 400, alan bazlı hata listesiyle
  - `NotFoundException` (Application) → 404
  - `DomainException` (Domain) → 422
  - Beklenmeyen hata → 500, iç ayrıntı (stack trace, mesaj) istemciye VERİLMEZ,
    sadece sunucuda loglanır
  - AuthenticationFailedException (Application) → 401
- İstemci davranışı (bkz. docs/specs/OfflineSync.md): 400, 404, 409, 422 kalıcı reddir (işlem
  kuyruktan atılır); 401 yetkisizliktir, kalıcı ret DEĞİLDİR (access token yenilenip tekrar denenir);
  408, 429, 5xx ve ağ hataları geçicidir (tekrar denenir). Bir kural/doğrulama hatası asla 5xx döndürülmez. Bu yüzden bir kural/doğrulama hatası
  asla 5xx döndürülmez.

## Yasaklar
- Repository, DbContext, Hosted Service referansı controller'da
  KULLANILMAZ; sadece MediatR üzerinden Application'a erişilir.
- Mobil istemcinin beklediği response şekli Application'daki DTO'lara
  birebir uyar; WebApi'de ekstra/gölge mapping YAPILMAZ.
- Hata mesajlarında başka bir kullanıcının kaydının varlığına dair bilgi
  sızdırılmaz (başkasının kaydı da 404 döner).

## Test Beklentisi
- API seviyesi test ihtiyacı doğarsa (örn. `WebApplicationFactory`), ayrı
  bir proje açılır; mevcut Test/IntegrationTests projelerine WebApi
  referansı EKLENMEZ.
- Exception → HTTP durum kodu eşlemesi yazıldığında her eşleme için bir
  test olması beklenir.