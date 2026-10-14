# Gamification — Özellik Spesifikasyonu

## Durum: Taslak (Domain tasarımı sürüyor)

## Kesinleşen Kurallar
- `Streak` kullanıcı bazlıdır (`UserId` alanı taşır), MVP'de tek kullanıcı
  olsa bile ileride çok kullanıcılı yapıya hazır tasarlanır.
- Bir kullanıcının tek bir Streak kaydı olur (Persistence’ta UserId unique index).
- Bir günün "seriyi devam ettirmesi" **ağırlıklı tamamlanma yüzdesi** ile
  belirlenir, basit (tamamlanan/toplam) oranla DEĞİL:
  - Normal görev = 1 puan, Önemli görev = 2 puan, Kritik görev = 3 puan
    (bkz. `TaskPriority` enum, Tasks/TaskPriority.cs)
  - Günün puanı = (tamamlanan görevlerin puan toplamı) / (o güne atanmış
    tüm görevlerin maksimum puan toplamı)
  - Bu hesaplama Application katmanında yapılır (Domain'e sadece sonuç
    yüzdesi parametre olarak girer — bkz. entity_rules.md Time Injection
    prensibiyle aynı mantık: "hangi günün puanı ne" kararı Domain'in işi
    değil).
- Eşik: ağırlıklı yüzde **%70 ve üzeri** ise seri devam eder/artar, altındaysa
  seri sıfırlanır (freeze hakkı yoksa — aşağıya bkz.).
- **Seri Dondurma (Freeze) — basitleştirilmiş v1 kapsamı:** `Streak`
  entity'si bir `AvailableFreezes` (int) sayacı taşır. Eşik tutturulamazsa
  ve `AvailableFreezes > 0` ise bir freeze harcanır, seri SIFIRLANMAZ ama
  ARTMAZ DA (olduğu yerde kalır). `AvailableFreezes` nereden kazanılır/satın
  alınır (elmas ekonomisi, premium üyelik) — bu proje kapsamında HENÜZ
  TASARLANMADI, bkz. TODO.
- Günlük değerlendirme idempotent olmalıdır: aynı gün için `EvaluateDay()`
  iki kez çağrılamaz (örn. Hosted Service yanlışlıkla iki kez tetiklenirse).
  Bu, `LastEvaluatedDate` alanıyla Domain seviyesinde garanti altına alınır.
- "Gün sonu", kullanıcının saat dilimine göre yerel 00:00'dır (`UserProfile`'daki
  IANA saat dilimi). Sunucu "hangi yerel gün bitti" kararını Application'da verir;
  Streak.EvaluateDay'e sadece sonuç gelir.
- Hiç (silinmemiş) görevi olmayan gün "boş gün"dür: seriyi artırmaz.
  Ardışık en fazla 2 boş güne izin verilir (seri korunur); 3. ardışık boş
  günde seri sıfırlanır. Görevi olan herhangi bir gün (başarılı veya
  başarısız) boş gün sayacını sıfırlar. Boş gün freeze harcamaz. Amaç:
  kullanıcıyı görev eklemeye teşvik etmek.
- Application her günü (boş olanlar dahil) kullanıcının yerel gün sırasına
  göre değerlendirir: görevi olan gün → `EvaluateDay`, boş gün →
  `EvaluateEmptyDay`. Gün atlanmaz; atlanırsa tarih sırası kontrolü hata verir.
- Gün değerlendirmesi `EvaluateUserDaysCommand` ile yapılır (Application,
  Features/Gamification/). Bir gün, kullanıcının yerel saatiyle ertesi gün
  06:00'da değerlendirilebilir hale gelir (bekleme süresi 6 saat, sabit:
  `DayEvaluationRules.GracePeriod`). Amaç: gece yarısına yakın offline
  yapılıp gecikmeli senkronize edilen tamamlamalar gün kilitlenmeden işlensin.
  Değerlendirilebilir son gün = `UserProfile.GetLocalDate(şimdi - 6 saat) - 1 gün`.
- Başlangıç günü: streak kaydı varsa `LastEvaluatedDate + 1`; kayıt yoksa
  (veya hiç değerlendirme yapılmamışsa) kullanıcının kayıt gününün yerel tarihi.
- Tek çalıştırmada başlangıçtan son değerlendirilebilir güne kadar TÜM günler
  sırayla değerlendirilir (kullanıcı günlerce uygulamayı açmasa da atlanan
  günler doldurulur): görevi olan gün → `EvaluateDay` (ağırlıklı yüzde),
  görevi olmayan gün → `EvaluateEmptyDay`. Silinmiş görevler sayılmaz.
  Görevler tek aralık sorgusuyla çekilir.
- Streak kaydı ilk değerlendirmede yoksa oluşturulur. Değerlendirilecek gün
  yoksa hiçbir şey yazılmaz ve görev sorgusuna gidilmez (idempotent).
- Sonuç: değerlendirilen gün sayısı ve güncel seri değerleri (bildirimler
  için, bkz. Notifications.md).

## Henüz Kararlaştırılmadı (TODO)
- [ ] `AvailableFreezes` nasıl kazanılır? (Elmas harcama, Premium üyelik,
  başka bir ödül mekanizması?) — v2.0 kapsamına ertelendi.
- [ ] Elmas ekonomisi / Premium üyelik ayrı bir aggregate mi olacak? —
  v2.0'da ayrıca tasarlanacak, bu dosyaya o zaman bağlanacak.
- [ ] `%70` eşiği sabit mi kalacak, yoksa ileride kullanıcı tarafından
  ayarlanabilir mi olacak?
- [ ] `EvaluateDay()`'i kim, ne zaman tetikler? (Muhtemelen Infrastructure'daki
  bir Hosted Service, her gün gece yarısı — Infrastructure katmanına
  geçildiğinde netleştirilecek.)
- [x] Gün sonu değerlendirmesi ile offline'da gecikmeli senkronize edilen
  tamamlamalar: kullanıcı 23:50'de offline tamamlayıp ertesi gün senkronize
  ederse, değerlendirme o tamamlamayı görmeden çalışmış olabilir. Bir
  bekleme süresi (grace period) gerekebilir. Infrastructure'da
  değerlendirme tetiklenirken karar verilecek.
- [ ] Maskot tepkileri (hangi eyleme hangi tepki), günlük özlü sözlerin kaynağı.
- [ ] Zamanlı akıllı bildirimlerin kuralları (Infrastructure'a geçerken).
- [ ] `Streak` için eşzamanlılık: aynı kullanıcı için iki değerlendirme aynı
  anda çalışırsa (iki sunucu örneği, üst üste binen zamanlayıcı) çakışma olur.
  Persistence'ta Streak'e satır sürümü (optimistic concurrency token) eklenecek;
  çakışmada işlem atlanır, bir sonraki turda tekrar denenir.
- [ ] Gün sonucu (başarılı/başarısız/dondurulmuş) özet bildiriminde gerekirse
  `Streak.EvaluateDay` bir sonuç tipi dönmeli (Domain değişikliği, bildirim
  tasarımıyla birlikte karar verilecek).

## İlgili Katmanlar
- Domain: `Streak` entity (bkz. Gamification/Streak.cs), `TaskPriority` enum
  (bkz. Tasks/TaskPriority.cs)
- Application: Günlük ağırlıklı puan hesaplama mantığı (henüz yazılmadı)
- Persistence: (henüz yok)
- Infrastructure: Günlük değerlendirmeyi tetikleyen Hosted Service (henüz yok)
- WebApi: (henüz yok)