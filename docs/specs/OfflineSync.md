# Offline Sync — Özellik Spesifikasyonu

## Durum: Taslak

## Kesinleşen Kurallar
- Push: istemci kuyruğundaki Command'lar sırayla tek tek gönderilir. Tüm
  Command'lar idempotent'tir (aynı istek tekrar gelirse hata verilmez).
- Pull: istemci "şu imleçten (cursor) beri değişenleri ver" der; sunucu
  değişen kayıtları ve silinmiş kayıtları (mezar taşı) döner.
- Çakışma: her Command yalnızca kendi alanlarına dokunur; sunucu her
  Command'ı Domain kurallarına göre doğrular. Aynı alana gelen sonraki
  komut kazanır.
- Yeniden deneme: ağ hatası, 5xx, 408 ve 429 geçicidir, artan bekleme ile tekrar denenir.
  400, 404, 409, 422 kalıcıdır: istemci işlemi kuyruktan atar, sunucu durumunu çeker ve kullanıcıya
  bildirir. 401 kalıcı DEĞİLDİR: istemci refresh token ile access token'ı yeniler ve işlemi tekrar
  dener; yenileme de 401 dönerse kuyruk BEKLEMEYE alınır (işlemler silinmez), kullanıcıdan giriş
  istenir ve girişten sonra kuyruk kaldığı yerden devam eder.
- Bir görev silindikten sonra gelen (gecikmeli) güncelleme istekleri 404 döner;
  istemci bunu kalıcı ret sayıp kuyruktan atar ve kullanıcıya hata göstermez.

## Henüz Kararlaştırılmadı (TODO)
- [ ] Pull imleci: zaman damgası yerine artan sürüm numarası (sequence) daha
  güvenli olabilir (eşzamanlı işlemlerde satır kaçırma riski). Persistence'ta karar.
- [ ] persistence_rules.md'deki `IsSynced` alanı istemci kavramı; sunucuda
  gerekmeyebilir, gözden geçirilecek.
- [ ] Komutların cihazlar arası sırası (`occurredAt` kullanılacak mı?).
- [ ] Mezar taşı temizleme süresi.
- [ ] Persistence'a senkron alanları (güncellenme zamanı, sürüm numarası) şimdilik
  eklenmedi; pull imleci kararıyla birlikte yeni bir migration ile eklenecek.