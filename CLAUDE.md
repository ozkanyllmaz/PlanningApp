# PlanningApp — Global Kurallar

Bağımlılık yönü: WebApi → (Persistence + Infrastructure) → Application → Domain.

## Fiziksel Proje Konumları
- Src/Core/PlanningApp.Domain
- Src/Core/PlanningApp.Application
- Src/External/PlanningApp.Persistence   (SADECE EF Core / DbContext / migration)
- Src/External/PlanningApp.Infrastructure (SADECE dış servisler: hosted service, bildirim, 3.parti)
- PlanningApp.WebApi
- Test/PlanningApp.Test

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
- Her yeni Command/Query Handler'ı için Test projesinde en az bir
  happy-path ve bir validation-failure senaryosu; Persistence/Infrastructure'a
  dokunan her yeni repository/hosted-service davranışı için
  IntegrationTests projesinde karşılığı yazılması beklenir.

## Spec Dosyası Türleri ve Nerede Aranır
1. **Katman spec'i** (`{Proje}/.specs/*.md`) — HER ZAMAN önce okunur.
   Guardrail niteliğindedir: o katmanda teknik olarak ne yapılamaz.
2. **Özellik spec'i** (`docs/specs/*.md`) — üzerinde çalıştığın Command/Query/
   entity hangi özelliğe aitse o dosya da okunur. İş kuralının NASIL
   çalıştığını tanımlar.
   - docs/specs/TaskManagement.md — görev CRUD, akıllı zaman/öncelik algılama, undo
   - docs/specs/WeeklyDailyPlanning.md — makro/mikro geçiş, sürükle-bırak, tamamlama kısıtı
   - docs/specs/OfflineSync.md — SQLite kuyruk, çakışma çözümü (henüz taslak)
   - docs/specs/LockScreenQuickAccess.md — kilit ekranı akışı (henüz taslak)
   - docs/specs/Gamification.md — streak, maskot, arşiv (henüz taslak)

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