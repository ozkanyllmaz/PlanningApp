# Task Management — Özellik Spesifikasyonu

## Durum: Taslak (Domain tasarımı sürüyor)

## Kesinleşen Kurallar
- Bir görev (PlanningTask) iki durumdan birindedir:
  - Havuzda (backlog): belirli bir güne atanmamış (`AssignedDate == null`).
  - Plana atanmış: belirli bir güne (`AssignedDate`) atanmış.
- Bir görev SADECE plana atanmışken ve mikro (günlük) görünümdeyken
  tamamlanabilir; havuzdayken veya haftalık (makro) görünümdeyken
  `Complete()` çağrılamaz — bu Domain seviyesinde bir invariant'tır.
- Görev atandığı gün (`AssignedDate`, gün bazlı) ile hatırlatma saati
  (`ReminderTime`, opsiyonel saat bazlı) birbirinden ayrı tutulur.
  `ReminderTime` sadece bir güne atanmış görevlerde set edilebilir;
  havuza dönen bir görevin hatırlatıcısı da temizlenir.
- Havuz ↔ Plan arası geçiş iki yönlüdür (`AssignToDate()` / `ReturnToPool()`),
  sürükle-bırak özelliği bunu gerektirir.
- Görev başlığı düzenlenebilir (`Rename()`), ancak tamamlanmış bir görevin
  başlığı değiştirilemez.
- Tamamlama geri alınabilir (`Uncomplete()` / undo) — ne kadar süre içinde
  geri alınabileceği HENÜZ KARARLAŞTIRILMADI (aşağıya bkz.).
- Her görevin bir `Priority` (TaskPriority: Normal/Important/Critical) değeri
  vardır, oluşturulurken zorunlu parametre olarak verilir (varsayılan değer
  YOK — her görev için bilinçli bir seçim yapılmalı).

## Henüz Kararlaştırılmadı (TODO)
- [ ] Undo penceresi: sınırsız mı, süre sınırlı mı (örn. 5 dk)?
- [ ] "Akıllı zaman/öncelik algılama" hangi kelimeleri/pattern'leri tanıyacak?
- [ ] Bir görev aynı anda birden fazla FocusArea (renk etiketi) alabilir mi?
- [x] ReminderTime eklendi — AssignedDate'ten bağımsız, opsiyonel TimeOnly

## İlgili Katmanlar
- Domain: `PlanningTask` entity (bkz. Tasks/PlanningTask.cs — referans implementasyon)
- Application: CreateTaskCommand, CompleteTaskCommand, UndoTaskCompletionCommand,
  AssignTaskToDateCommand, ReturnTaskToPoolCommand, RenameTaskCommand (henüz yazılmadı)
- Persistence: (henüz yok)
- WebApi: (henüz yok)