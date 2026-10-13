# Domain Katmanı Kuralları

## Yasaklar
- Entity Framework, Dapper veya herhangi bir ORM/veritabanı paketi referansı YASAK.
- DTO, ViewModel, API'ye özgü attribute'lar ([FromBody], [JsonPropertyName] vb.) YASAK.
- MediatR, FluentValidation gibi Application'a ait paketler YASAK.
- Dışarıdan (Infrastructure/Persistence) hiçbir servise doğrudan bağımlılık kurulamaz.
- `public set` kullanımı YASAK. Tüm entity property'leri `private set` olmalı;
  dışarıdan durum değişikliğine izin verilmemelidir.
- Null/boş kontrolü dahil TÜM validasyon hataları `DomainException` fırlatır.
  `ArgumentNullException`, `ArgumentException` gibi framework exception'ları
  Domain katmanında KULLANILMAZ — tek, tutarlı bir exception tipi olmalı ki
  WebApi'deki global handling tek bir catch bloğuyla hepsini yönetebilsin.
- Entity'ler parametresiz constructor İÇERMEZ. Tüm zorunlu alanlar, private
  constructor'ın parametre listesinde yer alır ve constructor içinde doğrudan
  atanır. Bu, hem `public set` yasağıyla hem de dilin kendi kısıtıyla
  (C#'ta `required` + `private set` birlikte kullanılamaz — CS9032) uyumlu
  tek yaklaşımdır. `required` modifier ve `null!` ile uyarı bastırma bu
  projede KULLANILMAZ.

## Zorunluluklar
- **Referans Implementasyon:** `PlanningTask.cs` bu katmanın kanonik örneğidir.
  Yeni entity yazarken bu dosyadaki `private set`, `static` factory method
  ve davranışsal metotlarla korunan pattern'i BİREBİR takip et.
- Bu katmandaki sınıflar SADECE POCO (Plain Old CLR Object)'dur.
- **Rich Domain Model:** Durum değişiklikleri doğrudan property atamasıyla
  değil, sadece anlamlı metotlar (örn. `AssignToDate()`, `ReturnToPool()`)
  üzerinden yapılmalıdır.
- Her entity kendi invariant'ını (bozulamaz iş kuralını) constructor, factory
  metod veya davranışsal metotlarında korur.
- **ID Üretimi:** Tüm entity ID'leri `Guid` tipindedir. ID'yi Domain KENDİSİ
  üretmez — static factory metodu ID'yi bir parametre olarak alır. ID üretim
  kararı (offline oluşturulan bir kayıt için mobil istemciden gelen ID mi,
  yoksa Application katmanının `Guid.NewGuid()` ile ürettiği bir ID mi)
  Application katmanına aittir.
- **Kullanıcı Sahipliği:** Kullanıcıya ait her entity (`PlanningTask`,
  `PlanningWeek`, `Streak`) bir `UserId` (Guid) taşır. `UserId` factory
  metoduna zorunlu parametre olarak verilir, `Guid.Empty` reddedilir ve
  oluşturulduktan sonra DEĞİŞTİRİLEMEZ (setter veya değiştiren metot yok).
  Domain "şu anki kullanıcı kim" bilgisini bilmez; sahiplik kontrolü
  (bu kayıt bu kullanıcıya mı ait) Application katmanının işidir.
- **Zaman Bağımlılığı (Time Injection):** Entity içindeki hiçbir metot
  `DateTime.Now` veya `DateTimeOffset.UtcNow` gibi sistem saatine DOĞRUDAN
  erişemez — ne okuma (örn. "bugün ne") ne de yazma (örn. bir timestamp
  property'sini set ederken) amacıyla. Zaman gerektiren her işlem, zamanı
  Application katmanından bir parametre (`asOfDate`, `occurredAt`,
  `createdAt` vb.) olarak alır. Bu, test edilebilirliği garanti altına alır
  ve "doğru an" kararını Domain dışına taşır.
- **Koleksiyonlar:** Koleksiyon tutan entity'lerde backing field
  `private readonly List<T>` olur, dışarıya SADECE `IReadOnlyCollection<T>`
  veya `IReadOnlyList<T>` olarak, `get`-only property ile açılır. Listeye
  ekleme/çıkarma SADECE davranışsal bir metotla (örn. `AddTask(...)`) yapılır,
  asla doğrudan koleksiyon üzerinden değil.
- **Soft Delete:** Silinebilen entity'ler `IsDeleted` ve `DeletedAt` taşır.
  Silinmiş bir entity'de durum değiştiren HİÇBİR metot çalışmaz
  (`DomainException`). Silme işlemi `Delete(deletedAt)` metoduyla yapılır.
- **Sayısal Sıralama Değeri:** `double` tipindeki sıralama alanları
  `double.IsFinite` ile doğrulanır (NaN ve sonsuz değerler reddedilir).
  Değerin nasıl hesaplanacağı (komşuların ortası vb.) istemcinin/Application'ın işidir.
- **Saat Dilimi:** `TimeZoneInfo` ile saat dilimi çevirisi yapılabilir
  (dışarıdan verilen an çevrilir); `TimeZoneInfo.Local`, `DateTime.Now`
  ve benzeri sistem saati okumaları yine YASAK.

**Özel İstisnalar:** İş kuralı ihlallerinde `System.Exception` veya
  `ArgumentException` yerine her zaman `PlanningApp.Domain.Common.DomainException`
  fırlatılmalıdır.
- `DomainException` SADECE bu projede (`PlanningApp.Domain/Common/`) tanımlanır,
  Application veya başka bir katmana taşınmaz (bkz. CLAUDE.md → Exception
  Stratejisi).
- Value Object'ler burada tanımlanır: `TaskPriority`, `FocusColor` (sabit 5 renk), `StreakCount` gibi.
- Domain event'leri (varsa, örn. `TaskCompletedDomainEvent`) burada
  TANIMLANIR ama dispatch/tetikleme mekanizması Infrastructure'ın işidir.
- Kümülatif havuz (backlog) ile haftalık plana atanmış görev arasındaki
  fark bir entity durumu (örn. `AssignedDate == null` ise havuzda) olarak
  Domain seviyesinde modellenmeli, UI seviyesinde değil.
- "Görev sadece günlük (mikro) ekranda tamamlanabilir" kısıtı, entity'nin
  `Complete()` metodunun kendi içinde kontrol ettiği bir invariant olmalı.

  ## Klasör Yapısı
- Entity'ler ve ilişkili Value Object'ler/Domain Event'ler, tek bir
  `Entities/` klasöründe DEĞİL, feature/aggregate bazlı klasörlerde tutulur
  (örn. `Tasks/PlanningTask.cs`, `Weeks/PlanningWeek.cs`). Klasör adı,
  ilgili `docs/specs/*.md` feature dosyasının konusuyla eşleşmelidir.

## Test Beklentisi
- Her invariant için PlanningApp.Test'te en az bir "kural ihlali reddedilir"
  senaryosu bulunmalı (örn. boş başlıkla görev oluşturma denemesi, havuzdaki
  görevi tamamlama denemesi).