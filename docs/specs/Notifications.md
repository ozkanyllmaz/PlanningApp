# Notifications — Özellik Spesifikasyonu

## Durum: Fikir aşaması (kesinleşmedi; Infrastructure ve mobil aşamasında tasarlanacak)

## Amaç
Maskot üzerinden, kullanıcıyı bildirime boğmadan zamanlı ve bağlama duyarlı
bildirimler: gün içi ilerleme ("şu kadar görevi tamamladın, şunlar kaldı"),
önemli/kritik görevler için ekstra hatırlatma, gün sonu değerlendirme özeti.

## İlkeler (öneri, kesinleşmedi)
- İki ayrı tür: (1) kullanıcının kendi kurduğu görev hatırlatıcıları
  (`ReminderTime`) kullanıcı isteğidir, sınırlamaya tabi DEĞİLDİR;
  (2) maskotun kendi başına gönderdikleri günlük üst sınıra ve sessiz saatlere tabidir.
- Spam önleme: maskot bildirimlerinde günde en fazla N adet (öneri: 3), sessiz
  saatler (öneri: yerel 22:00-08:00), kullanıcı tüm maskot bildirimlerini kapatabilir.
- Gereksiz bildirim yok: günün tüm görevleri tamamlandıysa veya o gün görevi
  yoksa "kalanlar" bildirimi gönderilmez; geçmiş ve kilitli günler için bildirim yok.
- Öncelik: bildirim hakkı kısıtlıysa Critical, sonra Important görevler öne geçer.
- Gün sonu özeti, değerlendirme tamamlandıktan sonra sabah tek bir bildirimle verilir.

## Henüz Kararlaştırılmadı (TODO)
- [ ] Bildirim nereden gelir: sunucu push'u (Hosted Service) mi, cihazda yerel
  zamanlanmış bildirim mi (çevrimdışı çalışır ama içerik gönderim anında
  hesaplanamaz), yoksa karışık mı? (Görev hatırlatıcıları için yerel,
  ilerleme/özet için sunucu push'u önerilir; mobil aşamasında doğrulanacak.)
- [ ] Cihaz kaydı: kullanıcı başına birden çok cihaz/push token'ı için yeni
  entity ve endpoint gerekir.
- [ ] Kullanıcı tercihleri (açık/kapalı, sessiz saatler, günlük üst sınır):
  `UserProfile`'a mı ayrı entity'ye mi?
- [ ] Bildirim zamanları ve metinleri (maskot tepkileri), Gamification.md'deki
  maskot TODO'suyla birlikte.
- [ ] Gönderilen bildirimlerin kaydı (günlük üst sınırı sayabilmek için).