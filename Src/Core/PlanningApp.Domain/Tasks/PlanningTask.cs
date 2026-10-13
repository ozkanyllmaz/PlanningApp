using System;
using PlanningApp.Domain.Common;

namespace PlanningApp.Domain.Tasks;

public sealed class PlanningTask
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public TaskPriority Priority { get; private set; }
    public FocusColor? FocusColor { get; private set; }
    public double SortOrder { get; private set; }
    public DateOnly? AssignedDate { get; private set; }
    public TimeOnly? ReminderTime { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private PlanningTask(Guid id, Guid userId, string title, TaskPriority priority, double sortOrder, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Priority = priority;
        FocusColor = null;
        SortOrder = sortOrder;
        AssignedDate = null;
        ReminderTime = null;
        IsCompleted = false;
        CreatedAt = createdAt;
        CompletedAt = null;
        IsDeleted = false;
        DeletedAt = null;
    }

    public static PlanningTask CreateInPool(Guid id, Guid userId, string title, TaskPriority priority, double sortOrder, DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Kullanıcı kimliği boş olamaz.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Görev başlığı boş olamaz.");

        EnsureValidSortOrder(sortOrder);

        return new PlanningTask(id, userId, title.Trim(), priority, sortOrder, createdAt);
    }

    public void ChangePriority(TaskPriority newPriority)
    {
        EnsureNotDeleted();

        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görevin önceliği değiştirilemez.");

        Priority = newPriority;
    }

    // null verilirse renk kaldırılır
    public void SetFocusColor(FocusColor? color)
    {
        EnsureNotDeleted();

        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görevin odak rengi değiştirilemez.");

        if (color is { } value && !Enum.IsDefined(value))
            throw new DomainException("Geçersiz odak rengi.");

        FocusColor = color;
    }

    // Aynı liste içinde (havuz veya aynı gün) yeniden sıralama
    public void Reorder(double newSortOrder)
    {
        EnsureNotDeleted();

        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görev yeniden sıralanamaz.");

        EnsureValidSortOrder(newSortOrder);
        SortOrder = newSortOrder;
    }

    public void AssignToDate(DateOnly date, double sortOrder)
    {
        EnsureNotDeleted();

        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görevin tarihi değiştirilemez.");

        EnsureValidSortOrder(sortOrder);

        AssignedDate = date;
        SortOrder = sortOrder;
    }

    public void SetReminder(TimeOnly time)
    {
        EnsureNotDeleted();

        if (AssignedDate is null)
            throw new DomainException("Hatırlatıcı sadece bir güne atanmış görevlere eklenebilir.");

        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir göreve hatırlatıcı eklenemez.");

        ReminderTime = time;
    }

    public void ClearReminder()
    {
        EnsureNotDeleted();
        ReminderTime = null;
    }

    public void ReturnToPool(double sortOrder)
    {
        EnsureNotDeleted();

        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görev havuza geri alınamaz.");

        EnsureValidSortOrder(sortOrder);

        AssignedDate = null;
        ReminderTime = null;
        SortOrder = sortOrder;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        EnsureNotDeleted();

        if (AssignedDate is null)
            throw new DomainException("Görev sadece plana atanmışken tamamlanabilir.");

        if (IsCompleted)
            throw new DomainException("Görev zaten tamamlanmış.");

        // completedAt istemcinin yerel offset'iyle gelir; DateTime bileşeni kullanıcının yerel gününü verir
        var assignedDate = AssignedDate.Value;
        var completedLocalDate = DateOnly.FromDateTime(completedAt.DateTime);

        if (completedLocalDate < assignedDate)
            throw new DomainException("Gelecekteki bir güne atanmış görev tamamlanamaz.");

        if (completedLocalDate > assignedDate)
            throw new DomainException("Geçmiş güne atanmış görev tamamlanamaz; görevi yeni bir güne kopyalayın.");

        IsCompleted = true;
        CompletedAt = completedAt;
    }

    public void Uncomplete()
    {
        EnsureNotDeleted();

        if (!IsCompleted)
            throw new DomainException("Tamamlanmamış bir görev geri alınamaz.");

        IsCompleted = false;
        CompletedAt = null;
    }

    public void Rename(string newTitle)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(newTitle))
            throw new DomainException("Görev başlığı boş olamaz.");

        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görevin başlığı değiştirilemez.");

        Title = newTitle.Trim();
    }

    // Orijinal görev değişmez; kopya yeni Id ile, odak rengi korunarak ve hatırlatıcısız olarak hedef güne atanır
    public PlanningTask CopyToDate(Guid newId, DateOnly targetDate, double sortOrder, DateTimeOffset createdAt)
    {
        EnsureNotDeleted();

        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görev kopyalanamaz.");

        var copy = new PlanningTask(newId, UserId, Title, Priority, sortOrder, createdAt);
        copy.FocusColor = FocusColor;
        copy.AssignToDate(targetDate, sortOrder);
        return copy;
    }

    // Soft delete: kayıt silinmez, işaretlenir (offline senkronda "silindi" bilgisi taşınabilsin diye)
    public void Delete(DateTimeOffset deletedAt)
    {
        if (IsDeleted)
            throw new DomainException("Görev zaten silinmiş.");

        IsDeleted = true;
        DeletedAt = deletedAt;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Silinmiş bir görev değiştirilemez.");
    }

    private static void EnsureValidSortOrder(double sortOrder)
    {
        if (!double.IsFinite(sortOrder))
            throw new DomainException("Sıralama değeri geçerli bir sayı olmalıdır.");
    }
}
