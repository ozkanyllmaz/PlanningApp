# PlanningApp — Global Kurallar

Bağımlılık yönü: WebApi → (Persistence + Infrastructure) → Application → Domain.

## Fiziksel Proje Konumları
- Src/Core/PlanningApp.Domain
- Src/Core/PlanningApp.Application
- Src/External/PlanningApp.Persistence   (SADECE EF Core / DbContext / migration)
- Src/External/PlanningApp.Infrastructure (SADECE dış servisler: hosted service, bildirim, 3.parti)
- PlanningApp.WebApi
- Test/PlanningApp.Test
- Test/PlanningApp.IntegrationTests

## MUTLAK KURAL — Katman Sınırı
Bulunduğun projenin .specs/ klasöründeki dosyayı oku ve SADECE onu uygula.
Diğer katmanların implementasyon detaylarını tahmin etme, sadece onların
public interface/contract'larına güven.

Persistence ve Infrastructure ayrı projelerdir: veri erişimi kodu asla
Infrastructure'a, arkaplan/bildirim kodu asla Persistence'a yazılmaz.

## Framework ve Dil Sürümü
- Tüm projeler `.NET 10` hedefler (`<TargetFramework>net10.0</TargetFramework>`).
- Sadece .NET 10 / C# en güncel dil sürümünde desteklenen, stabil (preview
  olmayan) özellikler kullanılabilir.
- Deprecated (kullanımdan kaldırılmış) API'ler, eski .NET Framework'e özgü
  pattern'ler (örn. eski `WebClient`, senkron I/O üzerinden async taklidi)
  KULLANILMAZ.
- Belirsiz kaldığın bir dil/framework özelliği varsa varsayım yapma,
  en yakın stabil ve yaygın kabul görmüş yaklaşımı tercih et.

## İsimlendirme ve Dil Kuralı
- Sınıf, interface, metod, değişken, property isimleri KESİNLİKLE İngilizce
  olacak (örn. `PlanningTask`, `CreateTaskCommand`, `GetWeeklyPlanQuery`).
- Yorum satırları (`//`, `///` XML doc dahil) Türkçe yazılacak.
- Commit mesajları Türkçe yazılacak (tip prefix'i İngilizce kalabilir:
  `feat: haftalık plan sorgusu eklendi` gibi).
- Karışık dil kullanımı (örn. `GorevOlustur` gibi Türkçe-İngilizce karışık
  isimlendirme) KABUL EDİLMEZ.

## Çalışma Biçimi
- Dosyaları Write ve Edit araçlarıyla oluştur ve düzenle; betikle (Python vb.) toplu üretme.
- Derleme hatalarını `.csproj`'a global `Using` ekleyerek çözme; ilgili dosyaya açık `using` ekle.
- "Altın örnek" olarak işaretlenen dosyalara istenmedikçe dokunma.
- Bitirmeden `dotnet build` ve `dotnet test` çalıştır.

## Gizli Bilgiler (Secrets)
- Gerçek şifre, API anahtarı, JWT imzalama anahtarı ve bağlantı dizesi şifreleri
  kod, `appsettings*.json`, spec, test, commit mesajı veya prompt içine YAZILMAZ.
- Gizli değerler yalnızca `IConfiguration` üzerinden okunur; değer eksikse
  uygulama başlangıçta hata verir, varsayılan şifre YOKTUR.
- Yerelde `dotnet user-secrets`, üretimde ortam değişkenleri veya barındırma
  ortamının gizli deposu kullanılır. `.env` yalnızca Docker Compose içindir,
  repoya GİRMEZ; repoda sahte değerli `.env.example` bulunur.
- Claude Code `.env`, `secrets.json` ve benzeri dosyaları OKUMAZ, gerekirse
  yer tutucu değer kullanır.
- Test ve design-time bağlantı bilgileri sahte/yer tutucudur.

## Test Kütüphaneleri ve Katmanlaşma
- Unit testler: `Test/PlanningApp.Test`
  - Referanslar: SADECE PlanningApp.Domain, PlanningApp.Application
  - Kapsam: Domain kuralları + Handler'lar, repository'ler MOCK'lanarak test edilir
- Entegrasyon testleri: `Test/PlanningApp.IntegrationTests`
  - Referanslar: PlanningApp.Application, PlanningApp.Persistence, PlanningApp.Infrastructure
  - Kapsam: Gerçek/in-memory PostgreSQL'e karşı DbContext ve Hosted Service davranışları
  - WebApi'ye BURADAN referans verilmez; API seviyesi test ihtiyacı doğarsa
    ayrı bir proje açılır.
- Her iki proje de framework olarak SADECE xUnit kullanır. Mocking: Moq,
  assertion: FluentAssertions. NUnit/MSTest ASLA eklenmez.
- FluentAssertions sürümü 7.2.0'a SABİTTİR (8.x ticari lisans gerektirir).
  Sürüm yükseltilmez, paket referansı elle değiştirilmez.
- Her yeni Command/Query Handler'ı için Test projesinde en az bir
  happy-path ve bir validation-failure senaryosu; Persistence/Infrastructure'a
  dokunan her yeni repository/hosted-service davranışı için
  IntegrationTests projesinde karşılığı yazılması beklenir.
- Entegrasyon testlerini çalıştırmak için Docker (Docker Desktop) açık olmalıdır.

## Spec Dosyası Türleri ve Nerede Aranır
1. **Katman spec'i** (`{Proje}/.specs/*.md`) — HER ZAMAN önce okunur.
   Guardrail niteliğindedir: o katmanda teknik olarak ne yapılamaz.
2. **Özellik spec'i** (`docs/specs/*.md`) — üzerinde çalıştığın Command/Query/
   entity hangi özelliğe aitse o dosya da okunur. İş kuralının NASIL
   çalıştığını tanımlar.
   - docs/specs/TaskManagement.md — görev CRUD, sıralama, silme, tamamlama, undo, kopyalama
   - docs/specs/WeeklyDailyPlanning.md — makro/mikro geçiş, sürükle-bırak, hafta oluşturma, tamamlama kısıtı
   - docs/specs/Gamification.md — streak, gün sonu değerlendirmesi, maskot, arşiv
   - docs/specs/Authentication.md — kayıt/giriş, JWT, refresh token (ilk dilim; Google girişi sonra)
   - docs/specs/OfflineSync.md — push/pull senkronizasyonu, çakışma çözümü (taslak)
   - docs/specs/LockScreenQuickAccess.md — kilit ekranı hızlı ekleme (araştırma aşamasında, kesinleşmedi)
   - docs/specs/Notifications.md — maskot bildirimleri (fikir aşaması, kesinleşmedi)

## Exception Stratejisi
Her katman kendi hata tipini kendi içinde tanımlar, başka katmana taşımaz
(bağımlılık yönü ihlal edilmez). Somut exception tipi ve adı, ilgili
katmanın kendi `.specs/` dosyasında belirtilir. Tüm exception'lar, WebApi'deki
global exception handling middleware'de TEK bir yerden yakalanıp uygun HTTP
status koduna çevrilir — "derli topluluk" kaynak kodun fiziksel konumunda
değil, bu merkezi yakalama noktasında sağlanır.

Çelişki durumunda katman spec'i (guardrail) her zaman önceliklidir.
Henüz doldurulmamış bir docs/specs/ dosyasıyla karşılaşırsan, o özelliğe
dair varsayım YAPMA — bana sor veya en dar/en basit uygulamayı seç ve
bunu yorum satırıyla işaretle (`// TODO: spec netleşmedi, bkz. docs/specs/X.md`).