using System;
using PlanningApp.Domain.Common;

namespace PlanningApp.Domain.Weeks;

public class PlanningWeek
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly WeekStartDate { get; private set; }
    public bool IsArchived { get; private set; }
    public decimal? CompletionPercentage { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    public DateOnly WeekEndDate => WeekStartDate.AddDays(6);

    private PlanningWeek(Guid id, Guid userId, DateOnly weekStartDate)
    {
        Id = id;
        UserId = userId;
        WeekStartDate = weekStartDate;
        IsArchived = false;
        CompletionPercentage = null;
        ArchivedAt = null;
    }

    public static PlanningWeek Create(Guid id, Guid userId, DateOnly weekStartDate)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Kullanıcı kimliği boş olamaz.");

        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            throw new DomainException("Hafta başlangıcı Pazartesi olmalıdır.");

        return new PlanningWeek(id, userId, weekStartDate);
    }

    public void Archive(decimal completionPercentage, DateOnly asOfDate, DateTimeOffset archivedAt)
    {
        if (IsArchived)
            throw new DomainException("Bu hafta zaten arşivlenmiş.");

        if (asOfDate <= WeekEndDate)
            throw new DomainException("Henüz bitmemiş bir hafta arşivlenemez.");

        if (completionPercentage is < 0 or > 100)
            throw new DomainException("Tamamlanma yüzdesi 0-100 aralığında olmalıdır.");

        CompletionPercentage = completionPercentage;
        IsArchived = true;
        ArchivedAt = archivedAt;
    }
}
