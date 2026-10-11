using PlanningApp.Domain.Common;

namespace PlanningApp.Domain.Gamification;

public sealed class Streak
{
    private const decimal RequiredCompletionPercentage = 70m;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateOnly? LastEvaluatedDate { get; private set; }
    public int AvailableFreezes { get; private set; }

    private Streak(Guid id, Guid userId)
    {
        Id = id;
        UserId = userId;
        CurrentStreak = 0;
        LongestStreak = 0;
        LastEvaluatedDate = null;
        AvailableFreezes = 0;
    }

    public static Streak Create(Guid id, Guid userId)
        => new(id, userId);

    public void EvaluateDay(decimal weightedCompletionPercentage, DateOnly date)
    {
        if (weightedCompletionPercentage is < 0 or > 100)
            throw new DomainException("Ağırlıklı tamamlanma yüzdesi 0-100 aralığında olmalıdır.");

        if (LastEvaluatedDate is not null && date <= LastEvaluatedDate)
            throw new DomainException("Bu gün için değerlendirme zaten yapılmış veya geçmişe dönük bir tarih verildi.");

        if (weightedCompletionPercentage >= RequiredCompletionPercentage)
        {
            CurrentStreak++;
            if (CurrentStreak > LongestStreak)
                LongestStreak = CurrentStreak;
        }
        else if (AvailableFreezes > 0)
        {
            AvailableFreezes--;
            // Seri ne sıfırlanır ne artar — olduğu yerde kalır.
        }
        else
        {
            CurrentStreak = 0;
        }

        LastEvaluatedDate = date;
    }

    public void GrantFreeze(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Eklenen freeze miktarı pozitif olmalıdır.");

        AvailableFreezes += amount;
    }
}