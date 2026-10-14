using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Gamification.Commands.EvaluateUserDays;
using PlanningApp.Domain.Gamification;
using PlanningApp.Domain.Tasks;
using PlanningApp.Domain.Users;
using Xunit;

namespace PlanningApp.Test.Features.Gamification.Commands.EvaluateUserDays;

public class EvaluateUserDaysCommandHandlerTests
{
    private static readonly Guid UserId = new("11111111-1111-1111-1111-111111111111");

    // Europe/Istanbul UTC+3 (yaz saati uygulaması yok)
    private const string TimeZoneId = "Europe/Istanbul";

    // Kayıt anı: 2026-01-02 22:30 UTC = İstanbul'da 2026-01-03 01:30 (UTC günü ile yerel gün farklı)
    private static readonly DateTimeOffset ProfileCreatedAt = new(2026, 1, 2, 22, 30, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset TaskCreatedAt = new(2025, 12, 20, 9, 0, 0, TimeSpan.FromHours(3));

    // ---- Bekleme süresi (grace period) sınırı ----

    [Fact]
    public async Task Handle_AtGracePeriodBoundary_EvaluatesPreviousLocalDay()
    {
        // Arrange: 03:00 UTC = yerel 06:00; 2026-01-07 artık değerlendirilebilir
        var streak = CreateStreakEvaluatedThrough(new DateOnly(2026, 1, 6));
        var fixture = CreateFixture(new DateTimeOffset(2026, 1, 8, 3, 0, 0, TimeSpan.Zero), streak: streak);

        // Act
        var result = await fixture.Handler.Handle(new EvaluateUserDaysCommand(UserId), CancellationToken.None);

        // Assert
        result.EvaluatedDayCount.Should().Be(1);
        streak.LastEvaluatedDate.Should().Be(new DateOnly(2026, 1, 7));
    }

    [Fact]
    public async Task Handle_OneMinuteBeforeGracePeriodBoundary_DoesNotEvaluateDay()
    {
        // Arrange: 02:59 UTC = yerel 05:59; 2026-01-07 henüz değerlendirilemez
        var streak = CreateStreakEvaluatedThrough(new DateOnly(2026, 1, 6));
        var fixture = CreateFixture(new DateTimeOffset(2026, 1, 8, 2, 59, 0, TimeSpan.Zero), streak: streak);

        // Act
        var result = await fixture.Handler.Handle(new EvaluateUserDaysCommand(UserId), CancellationToken.None);

        // Assert
        result.EvaluatedDayCount.Should().Be(0);
        streak.LastEvaluatedDate.Should().Be(new DateOnly(2026, 1, 6));
    }

    // ---- Değerlendirilecek gün yok ----

    [Fact]
    public async Task Handle_WithNoDayToEvaluate_SkipsTaskQueryAndSave()
    {
        // Arrange
        var streak = CreateStreakEvaluatedThrough(new DateOnly(2026, 1, 6));
        var fixture = CreateFixture(new DateTimeOffset(2026, 1, 8, 2, 59, 0, TimeSpan.Zero), streak: streak);

        // Act
        var result = await fixture.Handler.Handle(new EvaluateUserDaysCommand(UserId), CancellationToken.None);

        // Assert
        fixture.TaskRepository.Verify(
            repository => repository.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        result.CurrentStreak.Should().Be(streak.CurrentStreak);
        result.LongestStreak.Should().Be(streak.LongestStreak);
    }

    [Fact]
    public async Task Handle_WithNoStreakAndRegistrationDayNotYetEvaluable_WritesNothingAndReturnsZeros()
    {
        // Arrange: kayıt günü 2026-01-03; son değerlendirilebilir gün 2026-01-02
        var fixture = CreateFixture(new DateTimeOffset(2026, 1, 3, 10, 0, 0, TimeSpan.Zero), streak: null);

        // Act
        var result = await fixture.Handler.Handle(new EvaluateUserDaysCommand(UserId), CancellationToken.None);

        // Assert
        result.Should().Be(new EvaluateUserDaysResult(0, 0, 0));
        fixture.StreakRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Streak>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Çok günlük telafi ----

    [Fact]
    public async Task Handle_WithMultipleMissedDays_EvaluatesInOrderAndReturnsExpectedResult()
    {
        // Arrange: 01-01 değerlendirilmiş (seri 1); 01-02 boş, 01-03 görevli (%100), 01-04 boş
        var streak = CreateStreakEvaluatedThrough(new DateOnly(2026, 1, 1));
        var tasks = new[] { CreateTask(new DateOnly(2026, 1, 3), TaskPriority.Normal, completed: true) };
        var fixture = CreateFixture(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero), streak: streak, tasks: tasks);

        // Act
        var result = await fixture.Handler.Handle(new EvaluateUserDaysCommand(UserId), CancellationToken.None);

        // Assert
        result.Should().Be(new EvaluateUserDaysResult(EvaluatedDayCount: 3, CurrentStreak: 2, LongestStreak: 2));
        streak.LastEvaluatedDate.Should().Be(new DateOnly(2026, 1, 4));
        streak.ConsecutiveEmptyDays.Should().Be(1); // son gün boş
        fixture.TaskRepository.Verify(
            repository => repository.GetByDateRangeAsync(
                UserId, new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 4), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Streak kaydı yok ----

    [Fact]
    public async Task Handle_WithNoStreakRecord_CreatesStreakAndStartsFromRegistrationLocalDay()
    {
        // Arrange: kayıt günü yerel 2026-01-03 (UTC günü 01-02 değil); son değerlendirilebilir gün 2026-01-04
        Streak? addedStreak = null;
        var fixture = CreateFixture(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero), streak: null);
        fixture.StreakRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Streak>(), It.IsAny<CancellationToken>()))
            .Callback<Streak, CancellationToken>((streak, _) => addedStreak = streak)
            .Returns(Task.CompletedTask);

        // Act
        var result = await fixture.Handler.Handle(new EvaluateUserDaysCommand(UserId), CancellationToken.None);

        // Assert
        fixture.StreakRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Streak>(), It.IsAny<CancellationToken>()),
            Times.Once);
        addedStreak.Should().NotBeNull();
        addedStreak!.UserId.Should().Be(UserId);
        addedStreak.LastEvaluatedDate.Should().Be(new DateOnly(2026, 1, 4));
        result.EvaluatedDayCount.Should().Be(2);
        fixture.TaskRepository.Verify(
            repository => repository.GetByDateRangeAsync(
                UserId, new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 4), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithStreakThatWasNeverEvaluated_StartsFromRegistrationDayWithoutAddingStreak()
    {
        // Arrange: kayıt var ama LastEvaluatedDate null (ör. yalnızca freeze verilmiş)
        var streak = Streak.Create(Guid.NewGuid(), UserId);
        streak.GrantFreeze(1);
        var fixture = CreateFixture(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero), streak: streak);

        // Act
        var result = await fixture.Handler.Handle(new EvaluateUserDaysCommand(UserId), CancellationToken.None);

        // Assert: başlangıç profilin kayıt günü (yerel 2026-01-03), mevcut streak kullanılır
        fixture.TaskRepository.Verify(
            repository => repository.GetByDateRangeAsync(
                UserId, new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 4), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.StreakRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Streak>(), It.IsAny<CancellationToken>()),
            Times.Never);
        result.EvaluatedDayCount.Should().Be(2);
        streak.LastEvaluatedDate.Should().Be(new DateOnly(2026, 1, 4));
        streak.AvailableFreezes.Should().Be(1); // boş günler freeze harcamaz
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Seri kuralları ----

    [Fact]
    public async Task Handle_WithThreeConsecutiveEmptyDays_ResetsStreak()
    {
        // Arrange: seri 3; 01-01, 01-02, 01-03 görevsiz
        var streak = CreateStreakEvaluatedThrough(new DateOnly(2025, 12, 31), consecutiveSuccessDays: 3);
        var fixture = CreateFixture(new DateTimeOffset(2026, 1, 4, 10, 0, 0, TimeSpan.Zero), streak: streak);

        // Act
        var result = await fixture.Handler.Handle(new EvaluateUserDaysCommand(UserId), CancellationToken.None);

        // Assert
        result.EvaluatedDayCount.Should().Be(3);
        result.CurrentStreak.Should().Be(0);
        result.LongestStreak.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithCompletedTasksOnEvaluatedDay_IncreasesStreak()
    {
        // Arrange: 01-05 değerlendirilmiş (seri 1); 01-06'da tamamlanmış görev
        var streak = CreateStreakEvaluatedThrough(new DateOnly(2026, 1, 5));
        var tasks = new[] { CreateTask(new DateOnly(2026, 1, 6), TaskPriority.Important, completed: true) };
        var fixture = CreateFixture(new DateTimeOffset(2026, 1, 7, 10, 0, 0, TimeSpan.Zero), streak: streak, tasks: tasks);

        // Act
        var result = await fixture.Handler.Handle(new EvaluateUserDaysCommand(UserId), CancellationToken.None);

        // Assert
        result.Should().Be(new EvaluateUserDaysResult(EvaluatedDayCount: 1, CurrentStreak: 2, LongestStreak: 2));
    }

    // ---- UserProfile yok ----

    [Fact]
    public async Task Handle_WithMissingUserProfile_ThrowsNotFoundExceptionWithoutSaving()
    {
        // Arrange
        var fixture = CreateFixture(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero), streak: null, hasProfile: false);

        // Act
        var act = async () => await fixture.Handler.Handle(new EvaluateUserDaysCommand(UserId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Test Helpers ----

    private sealed record Fixture(
        EvaluateUserDaysCommandHandler Handler,
        Mock<IPlanningTaskRepository> TaskRepository,
        Mock<IStreakRepository> StreakRepository,
        Mock<IUnitOfWork> UnitOfWork);

    private static Fixture CreateFixture(
        DateTimeOffset now,
        Streak? streak,
        IReadOnlyList<PlanningTask>? tasks = null,
        bool hasProfile = true)
    {
        var userProfileRepository = new Mock<IUserProfileRepository>();
        userProfileRepository
            .Setup(repository => repository.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasProfile ? CreateProfile() : null);

        var streakRepository = new Mock<IStreakRepository>();
        streakRepository
            .Setup(repository => repository.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        var taskRepository = new Mock<IPlanningTaskRepository>();
        taskRepository
            .Setup(repository => repository.GetByDateRangeAsync(
                UserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks ?? []);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new EvaluateUserDaysCommandHandler(
            userProfileRepository.Object,
            streakRepository.Object,
            taskRepository.Object,
            unitOfWork.Object,
            new FakeTimeProvider(now));

        return new Fixture(handler, taskRepository, streakRepository, unitOfWork);
    }

    private static UserProfile CreateProfile()
        => UserProfile.Create(UserId, TimeZoneId, ProfileCreatedAt);

    // Streak'i verilen güne kadar ardışık başarılı (%100) günlerle ilerletir
    private static Streak CreateStreakEvaluatedThrough(DateOnly lastEvaluatedDate, int consecutiveSuccessDays = 1)
    {
        var streak = Streak.Create(Guid.NewGuid(), UserId);
        for (var i = consecutiveSuccessDays - 1; i >= 0; i--)
            streak.EvaluateDay(100m, lastEvaluatedDate.AddDays(-i));

        return streak;
    }

    private static PlanningTask CreateTask(DateOnly date, TaskPriority priority, bool completed)
    {
        var task = PlanningTask.CreateInPool(Guid.NewGuid(), UserId, "Örnek görev", priority, 1000d, TaskCreatedAt);
        task.AssignToDate(date, 1000d);
        if (completed)
            task.Complete(new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), TimeSpan.FromHours(3)));

        return task;
    }
}
