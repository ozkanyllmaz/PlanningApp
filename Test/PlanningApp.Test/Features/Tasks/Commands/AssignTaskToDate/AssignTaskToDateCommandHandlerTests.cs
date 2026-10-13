using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Tasks.Commands.AssignTaskToDate;
using PlanningApp.Domain.Common;
using PlanningApp.Domain.Gamification;
using PlanningApp.Domain.Tasks;
using PlanningApp.Domain.Users;
using PlanningApp.Domain.Weeks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.AssignTaskToDate;

public class AssignTaskToDateCommandHandlerTests
{
    // Sabit bir kullanıcı kimliği; testlerin rastgele değerlere bağımlı olmaması için
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");

    // Sabit an: 2026-01-07 10:00 UTC. Europe/Istanbul (UTC+3) için yerel gün 2026-01-07'dir.
    private static readonly DateTimeOffset Now = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly LocalToday = new(2026, 1, 7);

    // Cuma; haftasının Pazartesi'si 2026-01-05
    private static readonly DateOnly TargetDay = new(2026, 1, 9);
    private static readonly DateOnly TargetWeekStart = new(2026, 1, 5);

    private static readonly DateTimeOffset SampleCreatedAt = new(2026, 1, 5, 9, 0, 0, TimeSpan.FromHours(3));

    private const double SampleSortOrder = 1000d;
    private const double OtherSortOrder = 2500.5d;

    private const string LockMessage = "Değerlendirilmiş bir günün görev durumu değiştirilemez.";

    private readonly Mock<IPlanningTaskRepository> _taskRepository = new();
    private readonly Mock<IStreakRepository> _streakRepository = new();
    private readonly Mock<IPlanningWeekRepository> _weekRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly AssignTaskToDateCommandHandler _handler;

    public AssignTaskToDateCommandHandlerTests()
    {
        _currentUser.Setup(service => service.UserId).Returns(SampleUserId);
        SetupProfile("Europe/Istanbul");
        _handler = new AssignTaskToDateCommandHandler(
            _taskRepository.Object,
            _streakRepository.Object,
            _weekRepository.Object,
            _userProfileRepository.Object,
            _currentUser.Object,
            _unitOfWork.Object,
            _timeProvider);
    }

    // ---- Handle: başarılı durum ----

    [Fact]
    public async Task Handle_WithFutureTargetDate_AssignsTaskAndSavesChangesOnce()
    {
        // Arrange
        var task = CreateSampleTask();
        SetupTask(task);
        SetupStreak(CreateStreakEvaluatedOn(LocalToday.AddDays(-1)));
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, OtherSortOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        task.AssignedDate.Should().Be(TargetDay);
        task.SortOrder.Should().Be(OtherSortOrder);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTargetDateOfLocalToday_AssignsTask()
    {
        // Arrange
        var task = CreateSampleTask();
        SetupTask(task);
        var command = new AssignTaskToDateCommand(task.Id, LocalToday, SampleSortOrder); // sınır: tam bugün geçerli

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        task.AssignedDate.Should().Be(LocalToday);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutStreakRecord_AssignsTask()
    {
        // Arrange
        var task = CreateSampleTask();
        SetupTask(task);
        SetupStreak(null); // kullanıcının henüz streak kaydı yok
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, SampleSortOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        task.AssignedDate.Should().Be(TargetDay);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Handle: kullanıcı kimliği ----

    [Fact]
    public async Task Handle_Always_QueriesRepositoriesWithUserIdFromCurrentUserService()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        _currentUser.Setup(service => service.UserId).Returns(currentUserId);
        var task = CreateSampleTask();
        _taskRepository
            .Setup(repository => repository.GetByIdAsync(currentUserId, task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _userProfileRepository
            .Setup(repository => repository.GetByIdAsync(currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProfile("Europe/Istanbul", currentUserId));
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, SampleSortOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _taskRepository.Verify(
            repository => repository.GetByIdAsync(currentUserId, task.Id, It.IsAny<CancellationToken>()), Times.Once);
        _userProfileRepository.Verify(
            repository => repository.GetByIdAsync(currentUserId, It.IsAny<CancellationToken>()), Times.Once);
        _streakRepository.Verify(
            repository => repository.GetByUserIdAsync(currentUserId, It.IsAny<CancellationToken>()), Times.Once);
        _weekRepository.Verify(
            repository => repository.GetByStartDateAsync(currentUserId, TargetWeekStart, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Handle: bulunamadı ----

    [Fact]
    public async Task Handle_WhenTaskNotFound_ThrowsNotFoundExceptionAndSkipsSave()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var command = new AssignTaskToDateCommand(taskId, TargetDay, SampleSortOrder); // repository varsayılan olarak null döner

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"{nameof(PlanningTask)} bulunamadı: {taskId}");
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserProfileNotFound_ThrowsNotFoundExceptionAndSkipsSave()
    {
        // Arrange
        var task = CreateSampleTask();
        SetupTask(task);
        _userProfileRepository
            .Setup(repository => repository.GetByIdAsync(SampleUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, SampleSortOrder);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"{nameof(UserProfile)} bulunamadı: {SampleUserId}");
        task.AssignedDate.Should().BeNull();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Handle: yerel bugün kuralı ----

    [Fact]
    public async Task Handle_WithTargetDateBeforeLocalToday_ThrowsValidationExceptionAndKeepsTaskUnchanged()
    {
        // Arrange
        var task = CreateSampleTask();
        SetupTask(task);
        var command = new AssignTaskToDateCommand(task.Id, LocalToday.AddDays(-1), SampleSortOrder);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainKey(nameof(AssignTaskToDateCommand.Date))
            .WhoseValue.Should().Contain("Hedef gün bugünden önce olamaz.");
        task.AssignedDate.Should().BeNull();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Europe/Istanbul", 8)]  // 21:30 UTC → İstanbul'da yerel gün 2026-01-08
    [InlineData("America/New_York", 7)] // 21:30 UTC → New York'ta yerel gün 2026-01-07
    [InlineData("America/New_York", 8)]
    public async Task Handle_WithTargetDateOnOrAfterLocalToday_InUserTimeZone_AssignsTask(string timeZoneId, int targetDay)
    {
        // Arrange
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 7, 21, 30, 0, TimeSpan.Zero));
        SetupProfile(timeZoneId);
        var task = CreateSampleTask();
        SetupTask(task);
        var command = new AssignTaskToDateCommand(task.Id, new DateOnly(2026, 1, targetDay), SampleSortOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        task.AssignedDate.Should().Be(new DateOnly(2026, 1, targetDay));
    }

    [Theory]
    [InlineData("Europe/Istanbul", 7)]  // yerel gün 2026-01-08 iken 01-07 geçmişte kalır
    [InlineData("America/New_York", 6)] // yerel gün 2026-01-07 iken 01-06 geçmişte kalır
    public async Task Handle_WithTargetDateBeforeLocalToday_InUserTimeZone_ThrowsValidationException(string timeZoneId, int targetDay)
    {
        // Arrange
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 7, 21, 30, 0, TimeSpan.Zero));
        SetupProfile(timeZoneId);
        var task = CreateSampleTask();
        SetupTask(task);
        var command = new AssignTaskToDateCommand(task.Id, new DateOnly(2026, 1, targetDay), SampleSortOrder);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        task.AssignedDate.Should().BeNull();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Handle: gün kilidi ----

    [Fact]
    public async Task Handle_WhenTargetDayAlreadyEvaluated_ThrowsDomainExceptionAndKeepsTaskUnchanged()
    {
        // Arrange
        var task = CreateSampleTask();
        SetupTask(task);
        SetupStreak(CreateStreakEvaluatedOn(TargetDay)); // hedef günün değerlendirmesi yapılmış
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, SampleSortOrder);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage(LockMessage);
        task.AssignedDate.Should().BeNull();
        _weekRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningWeek>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCurrentAssignedDayAlreadyEvaluated_ThrowsDomainExceptionAndKeepsTaskUnchanged()
    {
        // Arrange
        var currentDay = LocalToday.AddDays(-1);
        var task = CreateAssignedTask(currentDay);
        SetupTask(task);
        SetupStreak(CreateStreakEvaluatedOn(currentDay)); // görevin mevcut günü değerlendirilmiş
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, SampleSortOrder);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage(LockMessage);
        task.AssignedDate.Should().Be(currentDay);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Handle: hafta ----

    [Fact]
    public async Task Handle_WhenWeekDoesNotExist_CreatesWeekWithGeneratedIdAndAddsIt()
    {
        // Arrange
        var task = CreateSampleTask();
        SetupTask(task);
        PlanningWeek? addedWeek = null;
        _weekRepository
            .Setup(repository => repository.AddAsync(It.IsAny<PlanningWeek>(), It.IsAny<CancellationToken>()))
            .Callback<PlanningWeek, CancellationToken>((week, _) => addedWeek = week)
            .Returns(Task.CompletedTask);
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, SampleSortOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        addedWeek.Should().NotBeNull();
        addedWeek!.Id.Should().NotBe(Guid.Empty);
        addedWeek.UserId.Should().Be(SampleUserId);
        addedWeek.WeekStartDate.Should().Be(TargetWeekStart);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWeekExists_DoesNotCreateWeekAgain()
    {
        // Arrange
        var task = CreateSampleTask();
        SetupTask(task);
        _weekRepository
            .Setup(repository => repository.GetByStartDateAsync(SampleUserId, TargetWeekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlanningWeek.Create(Guid.NewGuid(), SampleUserId, TargetWeekStart));
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, SampleSortOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        task.AssignedDate.Should().Be(TargetDay);
        _weekRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningWeek>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(9, 5)]   // Cuma → aynı haftanın Pazartesi'si
    [InlineData(11, 5)]  // Pazar → aynı haftanın Pazartesi'si
    [InlineData(12, 12)] // Pazartesi → kendisi
    public async Task Handle_WithTargetDate_LooksUpWeekByMondayOfTargetDay(int targetDay, int expectedWeekStartDay)
    {
        // Arrange
        var task = CreateSampleTask();
        SetupTask(task);
        var command = new AssignTaskToDateCommand(task.Id, new DateOnly(2026, 1, targetDay), SampleSortOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _weekRepository.Verify(
            repository => repository.GetByStartDateAsync(
                SampleUserId, new DateOnly(2026, 1, expectedWeekStartDay), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Handle: idempotency ----

    [Fact]
    public async Task Handle_WhenAlreadyAssignedToSameDateAndSortOrder_ReturnsWithoutSavingOrQueryingOthers()
    {
        // Arrange
        var task = CreateAssignedTask(TargetDay); // SortOrder: SampleSortOrder
        SetupTask(task);
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, SampleSortOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        task.AssignedDate.Should().Be(TargetDay);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _userProfileRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _streakRepository.Verify(
            repository => repository.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _weekRepository.Verify(
            repository => repository.GetByStartDateAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAlreadyAssignedToSameDateAndTargetDayEvaluated_ReturnsWithoutError()
    {
        // Arrange: ilk istek başarılı olmuş, gün sonra değerlendirilmiş; tekrar gelen istek hata vermemeli
        var task = CreateAssignedTask(TargetDay);
        SetupTask(task);
        SetupStreak(CreateStreakEvaluatedOn(TargetDay));
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, SampleSortOrder);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAssignedToSameDateWithDifferentSortOrder_UpdatesSortOrderAndSaves()
    {
        // Arrange
        var task = CreateAssignedTask(TargetDay); // SortOrder: SampleSortOrder
        SetupTask(task);
        var command = new AssignTaskToDateCommand(task.Id, TargetDay, OtherSortOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        task.SortOrder.Should().Be(OtherSortOrder);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Test Helpers ----

    private void SetupTask(PlanningTask task)
        => _taskRepository
            .Setup(repository => repository.GetByIdAsync(SampleUserId, task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

    private void SetupStreak(Streak? streak)
        => _streakRepository
            .Setup(repository => repository.GetByUserIdAsync(SampleUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

    private void SetupProfile(string timeZoneId)
        => _userProfileRepository
            .Setup(repository => repository.GetByIdAsync(SampleUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProfile(timeZoneId, SampleUserId));

    private static UserProfile CreateProfile(string timeZoneId, Guid userId)
        => UserProfile.Create(userId, timeZoneId, SampleCreatedAt);

    private static PlanningTask CreateSampleTask()
        => PlanningTask.CreateInPool(
            Guid.NewGuid(), SampleUserId, "Örnek görev", TaskPriority.Normal, SampleSortOrder, SampleCreatedAt);

    private static PlanningTask CreateAssignedTask(DateOnly date)
    {
        var task = CreateSampleTask();
        task.AssignToDate(date, SampleSortOrder);
        return task;
    }

    // Verilen günü değerlendirmiş gerçek bir Streak (LastEvaluatedDate = date)
    private static Streak CreateStreakEvaluatedOn(DateOnly date)
    {
        var streak = Streak.Create(Guid.NewGuid(), SampleUserId);
        streak.EvaluateDay(80m, date);
        return streak;
    }
}
