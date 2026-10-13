using FluentAssertions;
using PlanningApp.Domain.Common;
using PlanningApp.Domain.Gamification;
using Xunit;

namespace PlanningApp.Test.Gamification;

public class StreakTests
{
    // Sabit bir başlangıç günü; testlerin sistem saatine bağımlı olmaması için
    private static readonly DateOnly StartDate = new(2026, 1, 5);

    // Sabit bir kullanıcı kimliği; testlerin rastgele değerlere bağımlı olmaması için
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");

    // ---- Create ----

    [Fact]
    public void Create_WithValidIds_ReturnsEmptyStreak()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var streak = Streak.Create(id, userId);

        // Assert
        streak.Id.Should().Be(id);
        streak.UserId.Should().Be(userId);
        streak.CurrentStreak.Should().Be(0);
        streak.LongestStreak.Should().Be(0);
        streak.LastEvaluatedDate.Should().BeNull();
        streak.AvailableFreezes.Should().Be(0);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        // Arrange
        var emptyUserId = Guid.Empty;

        // Act
        var act = () => Streak.Create(Guid.NewGuid(), emptyUserId);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Kullanıcı kimliği boş olamaz.");
    }

    // ---- EvaluateDay: %70 eşiği ----

    [Theory]
    [InlineData(70)]    // tam eşik
    [InlineData(70.1)]  // eşiğin hemen üstü
    [InlineData(100)]
    public void EvaluateDay_WithPercentageAtOrAboveThreshold_IncrementsStreak(double percentage)
    {
        // Arrange
        var streak = CreateSampleStreak();

        // Act
        streak.EvaluateDay((decimal)percentage, StartDate);

        // Assert
        streak.CurrentStreak.Should().Be(1);
        streak.LastEvaluatedDate.Should().Be(StartDate);
    }

    [Fact]
    public void EvaluateDay_WithPercentageJustBelowThreshold_ResetsStreak()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(3);

        // Act
        streak.EvaluateDay(69.9m, NextDayAfter(3));

        // Assert
        streak.CurrentStreak.Should().Be(0);
    }

    // ---- EvaluateDay: freeze ----

    [Fact]
    public void EvaluateDay_BelowThresholdWithFreeze_ConsumesFreezeAndKeepsStreak()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(3);
        streak.GrantFreeze(2);

        // Act
        streak.EvaluateDay(40m, NextDayAfter(3));

        // Assert
        streak.CurrentStreak.Should().Be(3); // ne sıfırlanır ne artar
        streak.AvailableFreezes.Should().Be(1);
        streak.LastEvaluatedDate.Should().Be(NextDayAfter(3));
    }

    [Fact]
    public void EvaluateDay_BelowThresholdWithoutFreeze_ResetsStreakAndKeepsFreezeCountZero()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(3);

        // Act
        streak.EvaluateDay(40m, NextDayAfter(3));

        // Assert
        streak.CurrentStreak.Should().Be(0);
        streak.AvailableFreezes.Should().Be(0);
    }

    [Fact]
    public void EvaluateDay_AboveThresholdWithFreeze_DoesNotConsumeFreeze()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(1);
        streak.GrantFreeze(1);

        // Act
        streak.EvaluateDay(80m, NextDayAfter(1));

        // Assert
        streak.CurrentStreak.Should().Be(2);
        streak.AvailableFreezes.Should().Be(1);
    }

    // ---- EvaluateDay: idempotency ----

    [Fact]
    public void EvaluateDay_OnSameDateTwice_ThrowsDomainException()
    {
        // Arrange
        var streak = CreateSampleStreak();
        streak.EvaluateDay(80m, StartDate);

        // Act
        var act = () => streak.EvaluateDay(80m, StartDate);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Bu gün için değerlendirme zaten yapılmış veya geçmişe dönük bir tarih verildi.");
        streak.CurrentStreak.Should().Be(1);
    }

    [Fact]
    public void EvaluateDay_WithPastDate_ThrowsDomainException()
    {
        // Arrange
        var streak = CreateSampleStreak();
        streak.EvaluateDay(80m, StartDate.AddDays(5));

        // Act
        var act = () => streak.EvaluateDay(80m, StartDate);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Bu gün için değerlendirme zaten yapılmış veya geçmişe dönük bir tarih verildi.");
        streak.CurrentStreak.Should().Be(1);
        streak.LastEvaluatedDate.Should().Be(StartDate.AddDays(5));
    }

    // ---- EvaluateDay: yüzde aralığı ----

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-50)]
    [InlineData(100.1)]
    [InlineData(250)]
    public void EvaluateDay_WithPercentageOutOfRange_ThrowsDomainException(double invalidPercentage)
    {
        // Arrange
        var streak = CreateSampleStreak();

        // Act
        var act = () => streak.EvaluateDay((decimal)invalidPercentage, StartDate);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Ağırlıklı tamamlanma yüzdesi 0-100 aralığında olmalıdır.");
        streak.CurrentStreak.Should().Be(0);
        streak.LastEvaluatedDate.Should().BeNull();
    }

    // ---- EvaluateDay: LongestStreak ----

    [Fact]
    public void EvaluateDay_WhenCurrentExceedsLongest_UpdatesLongestStreak()
    {
        // Arrange
        var streak = CreateSampleStreak();

        // Act
        streak.EvaluateDay(80m, StartDate);
        streak.EvaluateDay(90m, StartDate.AddDays(1));
        streak.EvaluateDay(100m, StartDate.AddDays(2));

        // Assert
        streak.CurrentStreak.Should().Be(3);
        streak.LongestStreak.Should().Be(3);
    }

    [Fact]
    public void EvaluateDay_AfterStreakReset_KeepsLongestStreak()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(4);

        // Act
        streak.EvaluateDay(10m, NextDayAfter(4)); // freeze yok, seri sıfırlanır
        streak.EvaluateDay(80m, NextDayAfter(4).AddDays(1));

        // Assert
        streak.CurrentStreak.Should().Be(1);
        streak.LongestStreak.Should().Be(4);
    }

    // ---- GrantFreeze ----

    [Fact]
    public void GrantFreeze_WithPositiveAmount_IncreasesAvailableFreezes()
    {
        // Arrange
        var streak = CreateSampleStreak();

        // Act
        streak.GrantFreeze(2);
        streak.GrantFreeze(1);

        // Assert
        streak.AvailableFreezes.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void GrantFreeze_WithNonPositiveAmount_ThrowsDomainException(int invalidAmount)
    {
        // Arrange
        var streak = CreateSampleStreak();

        // Act
        var act = () => streak.GrantFreeze(invalidAmount);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Eklenen freeze miktarı pozitif olmalıdır.");
        streak.AvailableFreezes.Should().Be(0);
    }

    // ---- EnsureDayNotEvaluated ----

    [Fact]
    public void EnsureDayNotEvaluated_WithNoEvaluationYet_DoesNotThrow()
    {
        // Arrange
        var streak = CreateSampleStreak();

        // Act
        var act = () => streak.EnsureDayNotEvaluated(StartDate);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureDayNotEvaluated_WithDateAfterLastEvaluatedDate_DoesNotThrow()
    {
        // Arrange
        var streak = CreateSampleStreak();
        streak.EvaluateDay(80m, StartDate);

        // Act
        var act = () => streak.EnsureDayNotEvaluated(StartDate.AddDays(1));

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureDayNotEvaluated_OnLastEvaluatedDate_ThrowsDomainException()
    {
        // Arrange
        var streak = CreateSampleStreak();
        streak.EvaluateDay(80m, StartDate);

        // Act
        var act = () => streak.EnsureDayNotEvaluated(StartDate);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Değerlendirilmiş bir günün görev durumu değiştirilemez.");
    }

    [Fact]
    public void EnsureDayNotEvaluated_WithDateBeforeLastEvaluatedDate_ThrowsDomainException()
    {
        // Arrange
        var streak = CreateSampleStreak();
        streak.EvaluateDay(80m, StartDate.AddDays(3));

        // Act
        var act = () => streak.EnsureDayNotEvaluated(StartDate.AddDays(2));

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Değerlendirilmiş bir günün görev durumu değiştirilemez.");
    }

    // ---- EvaluateEmptyDay ----

    [Fact]
    public void EvaluateEmptyDay_ForFirstTwoConsecutiveEmptyDays_KeepsStreakWithoutIncreasing()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(3);

        // Act
        streak.EvaluateEmptyDay(NextDayAfter(3));
        streak.EvaluateEmptyDay(NextDayAfter(3).AddDays(1));

        // Assert
        streak.CurrentStreak.Should().Be(3);
        streak.ConsecutiveEmptyDays.Should().Be(2);
        streak.LastEvaluatedDate.Should().Be(NextDayAfter(3).AddDays(1));
    }

    [Fact]
    public void EvaluateEmptyDay_ForThirdConsecutiveEmptyDay_ResetsStreak()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(3);
        streak.EvaluateEmptyDay(NextDayAfter(3));
        streak.EvaluateEmptyDay(NextDayAfter(3).AddDays(1));

        // Act
        streak.EvaluateEmptyDay(NextDayAfter(3).AddDays(2));

        // Assert
        streak.CurrentStreak.Should().Be(0);
    }

    [Fact]
    public void EvaluateEmptyDay_AfterSuccessfulTaskDay_ResetsConsecutiveEmptyDayCounter()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(3);
        var firstEmptyDay = NextDayAfter(3);

        // Act
        streak.EvaluateEmptyDay(firstEmptyDay);
        streak.EvaluateEmptyDay(firstEmptyDay.AddDays(1));
        streak.EvaluateDay(100m, firstEmptyDay.AddDays(2)); // görevli ve başarılı gün
        var counterAfterTaskDay = streak.ConsecutiveEmptyDays;
        streak.EvaluateEmptyDay(firstEmptyDay.AddDays(3));
        streak.EvaluateEmptyDay(firstEmptyDay.AddDays(4));

        // Assert
        counterAfterTaskDay.Should().Be(0);
        streak.CurrentStreak.Should().Be(4); // 3 + görevli gün, boş günler seriyi sıfırlamadı
        streak.ConsecutiveEmptyDays.Should().Be(2);
    }

    [Fact]
    public void EvaluateEmptyDay_AfterFailedTaskDay_ResetsConsecutiveEmptyDayCounter()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(3);
        streak.GrantFreeze(1); // başarısız gün seriyi sıfırlamasın, sadece sayaç davranışı ölçülsün
        var firstEmptyDay = NextDayAfter(3);

        // Act
        streak.EvaluateEmptyDay(firstEmptyDay);
        streak.EvaluateEmptyDay(firstEmptyDay.AddDays(1));
        streak.EvaluateDay(10m, firstEmptyDay.AddDays(2)); // görevli ama başarısız gün
        var counterAfterTaskDay = streak.ConsecutiveEmptyDays;
        streak.EvaluateEmptyDay(firstEmptyDay.AddDays(3));
        streak.EvaluateEmptyDay(firstEmptyDay.AddDays(4));

        // Assert
        counterAfterTaskDay.Should().Be(0);
        streak.CurrentStreak.Should().Be(3);
        streak.ConsecutiveEmptyDays.Should().Be(2);
    }

    [Fact]
    public void EvaluateEmptyDay_WithAvailableFreeze_DoesNotConsumeFreeze()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(3);
        streak.GrantFreeze(2);

        // Act
        streak.EvaluateEmptyDay(NextDayAfter(3));

        // Assert
        streak.AvailableFreezes.Should().Be(2);
        streak.CurrentStreak.Should().Be(3);
    }

    [Fact]
    public void EvaluateEmptyDay_OnLastEvaluatedDate_ThrowsDomainException()
    {
        // Arrange
        var streak = CreateSampleStreak();
        streak.EvaluateEmptyDay(StartDate);

        // Act
        var act = () => streak.EvaluateEmptyDay(StartDate);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Bu gün için değerlendirme zaten yapılmış veya geçmişe dönük bir tarih verildi.");
        streak.ConsecutiveEmptyDays.Should().Be(1);
    }

    [Fact]
    public void EvaluateEmptyDay_WithPastDate_ThrowsDomainException()
    {
        // Arrange
        var streak = CreateSampleStreak();
        streak.EvaluateEmptyDay(StartDate.AddDays(5));

        // Act
        var act = () => streak.EvaluateEmptyDay(StartDate);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Bu gün için değerlendirme zaten yapılmış veya geçmişe dönük bir tarih verildi.");
        streak.ConsecutiveEmptyDays.Should().Be(1);
        streak.LastEvaluatedDate.Should().Be(StartDate.AddDays(5));
    }

    [Fact]
    public void EvaluateEmptyDay_DoesNotChangeLongestStreak()
    {
        // Arrange
        var streak = CreateStreakWithCurrentValue(4);

        // Act
        streak.EvaluateEmptyDay(NextDayAfter(4));
        var longestAfterFirstEmptyDay = streak.LongestStreak;
        streak.EvaluateEmptyDay(NextDayAfter(4).AddDays(1));
        streak.EvaluateEmptyDay(NextDayAfter(4).AddDays(2)); // üçüncü boş gün seriyi sıfırlar

        // Assert
        longestAfterFirstEmptyDay.Should().Be(4);
        streak.CurrentStreak.Should().Be(0);
        streak.LongestStreak.Should().Be(4);
    }

    // ---- Test Helpers ----

    private static Streak CreateSampleStreak()
        => Streak.Create(Guid.NewGuid(), SampleUserId);

    // Ardışık günlerde eşiği geçerek CurrentStreak değerini istenen sayıya getirir
    private static Streak CreateStreakWithCurrentValue(int days)
    {
        var streak = CreateSampleStreak();
        for (var i = 0; i < days; i++)
            streak.EvaluateDay(100m, StartDate.AddDays(i));

        return streak;
    }

    // CreateStreakWithCurrentValue(days) sonrasındaki ilk değerlendirilmemiş gün
    private static DateOnly NextDayAfter(int days)
        => StartDate.AddDays(days);
}
