# Weekly/Daily Planning — Özellik Spesifikasyonu

## Durum: Taslak (Domain tasarımı sürüyor)

## Kesinleşen Kurallar
- `PlanningWeek` gerçek bir Domain entity'dir, kendi tablosunda kalıcı satırı
  ve kendi ID'si (Guid, client-side üretilir) vardır.
- Bir hafta, sabit takvim haftasıdır: Pazartesi başlar, Pazar biter.
  `WeekStartDate` SADECE Pazartesi günü olabilir — bu bir Domain invariant'ıdır.
  `WeekEndDate` ayrıca saklanmaz, `WeekStartDate.AddDays(6)` olarak türetilir.
- `PlanningWeek`, içindeki `PlanningTask`'leri DOĞRUDAN tutmaz (koleksiyon
  yok). İlişki tek yönlüdür: bir `PlanningTask`'in `AssignedDate`'i, bir
  `PlanningWeek`'in `[WeekStartDate, WeekStartDate+6]` aralığına düşüyorsa
  o haftaya aittir. Bu sorgu Persistence katmanında (tarih aralığına göre
  filtreleme) yapılır, Domain'de bir referans/foreign-key olarak MODELLENMEZ.
- Arşiv özelliği bir **snapshot**'tır, canlı sorgu değildir: hafta sona
  erdiğinde tamamlanma yüzdesi hesaplanır ve `PlanningWeek` üzerinde
  KALICI olarak dondurulur (`CompletionPercentage`, `IsArchived`,
  `ArchivedAt`). Bunun nedeni: kullanıcı arşive baktığında "o hafta nasıl
  gitmiş" sorusuna, o hafta sonrasında yapılan olası veri düzeltmelerinden
  etkilenmeyen sabit bir cevap vermek.
- Bir hafta, kendi bitiş tarihi geçmeden arşivlenemez — bu bir Domain
  invariant'ıdır (`Archive()` metodu kontrol eder).
- Arşivlenmiş bir hafta tekrar arşivlenemez (idempotent değil, ikinci
  çağrı hata fırlatır).
- Bir `PlanningWeek` bir kullanıcıya aittir (`UserId`). Bir görev, AYNI
  `UserId`'ye sahip ve `AssignedDate`'i `[WeekStartDate, WeekStartDate+6]`
  aralığına düşen haftaya aittir. Aynı kullanıcı için aynı `WeekStartDate`
  ile ikinci bir hafta oluşturulamaz (benzersizlik, Persistence'ta unique
  index ile garanti edilecek).

## Henüz Kararlaştırılmadı (TODO)
- [ ] `PlanningWeek` ne zaman oluşturulur? (Kullanıcı ilk kez o haftaya bir
  görev atadığında Application katmanında "yoksa oluştur" mantığıyla mı,
  yoksa önceden tüm haftalar baştan mı oluşturulur?) — Application katmanına
  geçtiğimizde netleştirilecek.
- [ ] Arşivleme işlemini kim tetikler? (Muhtemelen Infrastructure'daki bir
  Hosted Service, her hafta sonunda otomatik çalışır — ama tamamlanma
  yüzdesini hesaplamak Persistence'tan veri ister, kararı veren Application
  katmanı olmalı.) — Infrastructure/Persistence katmanlarına geçildiğinde
  netleştirilecek.
- [ ] İki parmak hareketiyle makro görünüme geçiş sırasında kaç hafta
  önceden/sonradan gösterilecek? (UI/mobil tarafı ilgilendiriyor, Domain'i
  etkilemiyor.)

## İlgili Katmanlar
- Domain: `PlanningWeek` entity (bkz. Weeks/PlanningWeek.cs)
- Application: (henüz yazılmadı — yukarıdaki TODO'lara bağlı)
- Persistence: (henüz yok)
- WebApi: (henüz yok)