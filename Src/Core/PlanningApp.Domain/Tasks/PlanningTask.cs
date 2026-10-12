using System;
using PlanningApp.Domain.Common;

namespace PlanningApp.Domain.Tasks;

public class PlanningTask
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateOnly? AssignedDate { get; private set; }
    public TimeOnly? ReminderTime { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private PlanningTask(Guid id, Guid userId, string title, TaskPriority priority, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Priority = priority;
        AssignedDate = null;
        ReminderTime = null;
        IsCompleted = false;
        CreatedAt = createdAt;
        CompletedAt = null;
    }

    public static PlanningTask CreateInPool(Guid id, Guid userId, string title, TaskPriority priority, DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Kullanıcı kimliği boş olamaz.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Görev başlığı boş olamaz.");

        return new PlanningTask(id, userId, title.Trim(), priority, createdAt);
    }

    public void ChangePriority(TaskPriority newPriority)
    {
        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görevin önceliği değiştirilemez.");

        Priority = newPriority;
    }

    public void AssignToDate(DateOnly date)
    {
        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görevin tarihi değiştirilemez.");

        AssignedDate = date;
    }

    public void SetReminder(TimeOnly time)
    {
        if (AssignedDate is null)
            throw new DomainException("Hatırlatıcı sadece bir güne atanmış görevlere eklenebilir.");

        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir göreve hatırlatıcı eklenemez.");

        ReminderTime = time;
    }

    public void ClearReminder() => ReminderTime = null;

    public void ReturnToPool()
    {
        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görev havuza geri alınamaz.");

        AssignedDate = null;
        ReminderTime = null;
    }

    public void Complete(DateTimeOffset completedAt)
    {
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
        if (!IsCompleted)
            throw new DomainException("Tamamlanmamış bir görev geri alınamaz.");

        IsCompleted = false;
        CompletedAt = null;
    }

    public void Rename(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new DomainException("Görev başlığı boş olamaz.");

        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görevin başlığı değiştirilemez.");

        Title = newTitle.Trim();
    }

    // Orijinal görev değişmez; kopya yeni Id ile, hatırlatıcısız olarak hedef güne atanır
    public PlanningTask CopyToDate(Guid newId, DateOnly targetDate, DateTimeOffset createdAt)
    {
        if (IsCompleted)
            throw new DomainException("Tamamlanmış bir görev kopyalanamaz.");

        var copy = new PlanningTask(newId, UserId, Title, Priority, createdAt);
        copy.AssignToDate(targetDate);
        return copy;
    }
}
