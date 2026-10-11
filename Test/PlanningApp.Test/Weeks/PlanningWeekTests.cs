using FluentAssertions;
using PlanningApp.Domain.Common;
using PlanningApp.Domain.Weeks;
using Xunit;

namespace PlanningApp.Test.Weeks;

public class PlanningWeekTests
{
    // Sabit bir Pazartesi (2026-01-05); testlerin sistem saatine bağımlı olmaması için
    private static readonly DateOnly Monday = new(2026, 1, 5);

    // Sabit bir kullanıcı kimliği; testlerin rastgele değerlere bağımlı olmaması için
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");

    // ---- Create ----

    [Fact]
    public void Create_WithMonday_ReturnsActiveWeek()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var week = PlanningWeek.Create(id, SampleUserId, Monday);

        // Assert
        week.Id.Should().Be(id);
        week.WeekStartDate.Should().Be(Monday);
        week.WeekEndDate.Should().Be(Monday.AddDays(6));
        week.IsArchived.Should().BeFalse();
        week.CompletionPercentage.Should().BeNull();
        week.ArchivedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(1)] // Salı
    [InlineData(2)] // Çarşamba
    [InlineData(3)] // Perşembe
    [InlineData(4)] // Cuma
    [InlineData(5)] // Cumartesi
    [InlineData(6)] // Pazar
    public void Create_WithNonMonday_ThrowsDomainException(int daysAfterMonday)
    {
        // Arrange
        var id = Guid.NewGuid();
        var notMonday = Monday.AddDays(daysAfterMonday);

        // Act
        var act = () => PlanningWeek.Create(id, SampleUserId, notMonday);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Hafta başlangıcı Pazartesi olmalıdır.");
    }

    [Fact]
    public void Create_WithGivenUserId_StoresUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var week = PlanningWeek.Create(Guid.NewGuid(), userId, Monday);

        // Assert
        week.UserId.Should().Be(userId);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        // Arrange
        var emptyUserId = Guid.Empty;

        // Act
        var act = () => PlanningWeek.Create(Guid.NewGuid(), emptyUserId, Monday);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Kullanıcı kimliği boş olamaz.");
    }

    // ---- Archive: bitmiş hafta kontrolü ----

    [Theory]
    [InlineData(7)]  // bir sonraki Pazartesi
    [InlineData(30)] // aylar sonrası
    public void Archive_AfterWeekEnded_ArchivesWeek(int daysAfterWeekStart)
    {
        // Arrange
        var week = CreateSampleWeek();
        var asOfDate = Monday.AddDays(daysAfterWeekStart);
        var archivedAt = DateTimeOffset.UtcNow;

        // Act
        week.Archive(75m, asOfDate, archivedAt);

        // Assert
        week.IsArchived.Should().BeTrue();
        week.CompletionPercentage.Should().Be(75m);
        week.ArchivedAt.Should().Be(archivedAt);
    }

    [Theory]
    [InlineData(0)] // haftanın ilk günü
    [InlineData(3)] // haftanın ortası
    [InlineData(6)] // haftanın son günü (Pazar) — hafta henüz bitmedi
    public void Archive_BeforeWeekEnded_ThrowsDomainException(int daysAfterWeekStart)
    {
        // Arrange
        var week = CreateSampleWeek();
        var asOfDate = Monday.AddDays(daysAfterWeekStart);

        // Act
        var act = () => week.Archive(50m, asOfDate, DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Henüz bitmemiş bir hafta arşivlenemez.");
        week.IsArchived.Should().BeFalse();
    }

    // ---- Archive: zaten arşivlenmiş kontrolü ----

    [Fact]
    public void Archive_OnActiveWeek_SetsIsArchivedTrue()
    {
        // Arrange
        var week = CreateSampleWeek();

        // Act
        week.Archive(100m, AfterWeekEnd(), DateTimeOffset.UtcNow);

        // Assert
        week.IsArchived.Should().BeTrue();
    }

    [Fact]
    public void Archive_OnAlreadyArchivedWeek_ThrowsDomainException()
    {
        // Arrange
        var week = CreateArchivedWeek();

        // Act
        var act = () => week.Archive(80m, AfterWeekEnd(), DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Bu hafta zaten arşivlenmiş.");
    }

    [Fact]
    public void Archive_OnAlreadyArchivedWeek_DoesNotOverwriteExistingValues()
    {
        // Arrange
        var week = CreateArchivedWeek(); // 60 ile arşivlendi
        var originalArchivedAt = week.ArchivedAt;

        // Act
        var act = () => week.Archive(90m, AfterWeekEnd(), DateTimeOffset.UtcNow.AddDays(1));

        // Assert
        act.Should().Throw<DomainException>();
        week.CompletionPercentage.Should().Be(60m);
        week.ArchivedAt.Should().Be(originalArchivedAt);
    }

    // ---- Archive: yüzde aralığı ----

    [Theory]
    [InlineData(0)]
    [InlineData(33.5)]
    [InlineData(100)]
    public void Archive_WithPercentageInRange_StoresPercentage(double percentage)
    {
        // Arrange
        var week = CreateSampleWeek();
        var expected = (decimal)percentage;

        // Act
        week.Archive(expected, AfterWeekEnd(), DateTimeOffset.UtcNow);

        // Assert
        week.CompletionPercentage.Should().Be(expected);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-50)]
    [InlineData(100.1)]
    [InlineData(250)]
    public void Archive_WithPercentageOutOfRange_ThrowsDomainException(double invalidPercentage)
    {
        // Arrange
        var week = CreateSampleWeek();

        // Act
        var act = () => week.Archive((decimal)invalidPercentage, AfterWeekEnd(), DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanma yüzdesi 0-100 aralığında olmalıdır.");
        week.IsArchived.Should().BeFalse();
        week.CompletionPercentage.Should().BeNull();
    }

    // ---- Test Helpers ----

    private static PlanningWeek CreateSampleWeek()
        => PlanningWeek.Create(Guid.NewGuid(), SampleUserId, Monday);

    // Haftanın bittiği ilk gün (bir sonraki Pazartesi)
    private static DateOnly AfterWeekEnd()
        => Monday.AddDays(7);

    private static PlanningWeek CreateArchivedWeek()
    {
        var week = CreateSampleWeek();
        week.Archive(60m, AfterWeekEnd(), DateTimeOffset.UtcNow);
        return week;
    }
}
