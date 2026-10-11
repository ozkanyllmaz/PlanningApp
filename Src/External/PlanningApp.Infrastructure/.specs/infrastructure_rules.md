# Infrastructure Katmanı Kuralları

## Kapsam (SADECE bunlar)
- `.NET Hosted Services`: streak/seri hesaplama arkaplan işi, günlük
  özlü söz yenileme, zamanlı bildirim tetikleme.
- Maskot'un "zamanlı akıllı bildirimler" göndermesini tetikleyen servisler.
- Push notification / e-posta gibi üçüncü parti entegrasyon istemcileri.
- Application katmanında tanımlanan dış-servis interface'lerinin
  (`INotificationTrigger` vb.) somut implementasyonları.

## Yasaklar
- Veri erişimi/DbContext kodu KESİNLİKLE YASAK — bu Persistence'ın işi.
  Infrastructure, veriye ihtiyaç duyarsa Persistence'ın sunduğu
  repository/interface üzerinden (Application aracılığıyla) erişir,
  kendi DbContext'ini asla açmaz.
- İş kuralı YAZILMAZ; sadece Application'ın verdiği kararı dış dünyaya
  (bildirim, arkaplan tetikleme) yansıtır.

## Test Beklentisi
- Hosted Service davranışları PlanningApp.IntegrationTests'te test edilir.