using PlanningApp.Domain.Common;

namespace PlanningApp.Domain.Gamification;

public sealed class Streak
{
    private const decimal RequiredCompletionPercentage = 70m;
    private const int MaxConsecutiveEmptyDays = 2;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateOnly? LastEvaluatedDate { get; private set; }
    public int AvailableFreezes { get; private set; }
    public int ConsecutiveEmptyDays { get; private set; }

    private Streak(Guid id, Guid userId)
    {
        Id = id;
        UserId = userId;
        CurrentStreak = 0;
        LongestStreak = 0;
        LastEvaluatedDate = null;
        AvailableFreezes = 0;
        ConsecutiveEmptyDays = 0;
    }

    public static Streak Create(Guid id, Guid userId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Kullanıcı kimliği boş olamaz.");

        return new Streak(id, userId);
    }

    // Görevi olan günün değerlendirmesi
    public void EvaluateDay(decimal weightedCompletionPercentage, DateOnly date)
    {
        if (weightedCompletionPercentage is < 0 or > 100)
            throw new DomainException("Ağırlıklı tamamlanma yüzdesi 0-100 aralığında olmalıdır.");

        EnsureDateAfterLastEvaluation(date);

        if (weightedCompletionPercentage >= RequiredCompletionPercentage)
        {
            CurrentStreak++;
            if (CurrentStreak > LongestStreak)
                LongestStreak = CurrentStreak;
        }
        else if (AvailableFreezes > 0)
        {
            AvailableFreezes--;
            // Seri ne sıfırlanır ne artar, olduğu yerde kalır.
        }
        else
        {
            CurrentStreak = 0;
        }

        ConsecutiveEmptyDays = 0; // görevi olan gün boş gün serisini keser
        LastEvaluatedDate = date;
    }

    // Hiç (silinmemiş) görevi olmayan günün değerlendirmesi: seri artmaz,
    // ardışık en fazla MaxConsecutiveEmptyDays boş güne izin verilir, aşılırsa seri sıfırlanır
    public void EvaluateEmptyDay(DateOnly date)
    {
        EnsureDateAfterLastEvaluation(date);

        ConsecutiveEmptyDays++;

        if (ConsecutiveEmptyDays > MaxConsecutiveEmptyDays)
            CurrentStreak = 0;

        LastEvaluatedDate = date;
    }

    public void GrantFreeze(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Eklenen freeze miktarı pozitif olmalıdır.");

        AvailableFreezes += amount;
    }

    // Verilen gün değerlendirilmiş mi (son değerlendirilen güne eşit veya ondan önceyse evet)
    public bool IsDayEvaluated(DateOnly date)
        => LastEvaluatedDate is not null && date <= LastEvaluatedDate;

    // Günün değerlendirmesi yapılmışsa o güne ait görev durumları artık değiştirilemez
    public void EnsureDayNotEvaluated(DateOnly date)
    {
        if (IsDayEvaluated(date))
            throw new DomainException("Değerlendirilmiş bir günün görev durumu değiştirilemez.");
    }

    private void EnsureDateAfterLastEvaluation(DateOnly date)
    {
        if (LastEvaluatedDate is not null && date <= LastEvaluatedDate)
            throw new DomainException("Bu gün için değerlendirme zaten yapılmış veya geçmişe dönük bir tarih verildi.");
    }
}