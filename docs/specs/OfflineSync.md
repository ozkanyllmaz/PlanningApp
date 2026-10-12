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
- Yeniden deneme: ağ hatası ve 5xx tekrar denenir; 4xx (kural/doğrulama
  reddi) kalıcıdır, istemci işlemi kuyruktan atar, sunucu durumunu çeker
  ve kullanıcıya bildirir.

## Henüz Kararlaştırılmadı (TODO)
- [ ] Pull imleci: zaman damgası yerine artan sürüm numarası (sequence) daha
  güvenli olabilir (eşzamanlı işlemlerde satır kaçırma riski). Persistence'ta karar.
- [ ] persistence_rules.md'deki `IsSynced` alanı istemci kavramı; sunucuda
  gerekmeyebilir, gözden geçirilecek.
- [ ] Komutların cihazlar arası sırası (`occurredAt` kullanılacak mı?).
- [ ] Mezar taşı temizleme süresi.