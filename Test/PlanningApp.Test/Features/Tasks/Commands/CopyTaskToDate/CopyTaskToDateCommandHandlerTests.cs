using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Tasks.Commands.CopyTaskToDate;
using PlanningApp.Domain.Common;
using PlanningApp.Domain.Gamification;
using PlanningApp.Domain.Tasks;
using PlanningApp.Domain.Users;
using PlanningApp.Domain.Weeks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.CopyTaskToDate;

public class CopyTaskToDateCommandHandlerTests
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
    private static readonly DateTimeOffset CopyCreatedAt = new(2026, 1, 7, 12, 0, 0, TimeSpan.FromHours(3));

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
    private readonly CopyTaskToDateCommandHandler _handler;

    public CopyTaskToDateCommandHandlerTests()
    {
        _currentUser.Setup(service => service.UserId).Returns(SampleUserId);
        SetupProfile("Europe/Istanbul");
        _handler = new CopyTaskToDateCommandHandler(
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
    public async Task Handle_WithFutureTargetDate_AddsCopyAndSavesChangesOnce()
    {
        // Arrange
        var source = CreateSourceTask();
        SetupSource(source);
        SetupStreak(CreateStreakEvaluatedOn(LocalToday.AddDays(-1)));
        PlanningTask? addedTask = null;
        CaptureAddedTask(task => addedTask = task);
        var command = CreateCommand(source.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(command.NewId);
        addedTask.Should().NotBeNull();
        addedTask!.Id.Should().Be(command.NewId);
        addedTask.UserId.Should().Be(SampleUserId);
        addedTask.Title.Should().Be(source.Title);
        addedTask.Priority.Should().Be(source.Priority);
        addedTask.AssignedDate.Should().Be(TargetDay);
        addedTask.SortOrder.Should().Be(OtherSortOrder);
        addedTask.CreatedAt.Should().Be(CopyCreatedAt);
        addedTask.IsCompleted.Should().BeFalse();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTargetDateOfLocalToday_AddsCopy()
    {
        // Arrange
        var source = CreateSourceTask();
        SetupSource(source);
        var command = CreateCommand(source.Id) with { TargetDate = LocalToday }; // sınır: tam bugün geçerli

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(command.NewId);
        _taskRepository.Verify(
            repository => repository.AddAsync(
                It.Is<PlanningTask>(task => task.AssignedDate == LocalToday), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutStreakRecord_AddsCopy()
    {
        // Arrange
        var source = CreateSourceTask();
        SetupSource(source);
        SetupStreak(null); // kullanıcının henüz streak kaydı yok
        var command = CreateCommand(source.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSourceDayAlreadyEvaluated_StillAddsCopy()
    {
        // Arrange: kaynak görev değişmediği için kaynağın günü kilitli olsa da kopyalama çalışır
        var sourceDay = LocalToday.AddDays(-1);
        var source = CreateSourceTask(sourceDay);
        SetupSource(source);
        SetupStreak(CreateStreakEvaluatedOn(sourceDay));
        var command = CreateCommand(source.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        source.AssignedDate.Should().Be(sourceDay);
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSourceIsCompleted_ThrowsDomainExceptionAndSkipsAdd()
    {
        // Arrange
        var sourceDay = new DateOnly(2026, 1, 6);
        var source = CreateSourceTask(sourceDay);
        source.Complete(new DateTimeOffset(2026, 1, 6, 10, 0, 0, TimeSpan.FromHours(3)));
        SetupSource(source);
        var command = CreateCommand(source.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Tamamlanmış bir görev kopyalanamaz.");
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Handle: kullanıcı kimliği ----

    [Fact]
    public async Task Handle_Always_QueriesRepositoriesWithUserIdFromCurrentUserService()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        _currentUser.Setup(service => service.UserId).Returns(currentUserId);
        var source = CreateSourceTask();
        _taskRepository
            .Setup(repository => repository.GetByIdAsync(currentUserId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _userProfileRepository
            .Setup(repository => repository.GetByIdAsync(currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfile.Create(currentUserId, "Europe/Istanbul", SampleCreatedAt));
        var command = CreateCommand(source.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _taskRepository.Verify(
            repository => repository.GetByIdIncludingDeletedAsync(currentUserId, command.NewId, It.IsAny<CancellationToken>()),
            Times.Once);
        _taskRepository.Verify(
            repository => repository.GetByIdAsync(currentUserId, source.Id, It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task Handle_WhenSourceTaskNotFound_ThrowsNotFoundExceptionAndSkipsSave()
    {
        // Arrange
        var sourceTaskId = Guid.NewGuid();
        var command = CreateCommand(sourceTaskId); // repository varsayılan olarak null döner

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"{nameof(PlanningTask)} bulunamadı: {sourceTaskId}");
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserProfileNotFound_ThrowsNotFoundExceptionAndSkipsSave()
    {
        // Arrange
        var source = CreateSourceTask();
        SetupSource(source);
        _userProfileRepository
            .Setup(repository => repository.GetByIdAsync(SampleUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var command = CreateCommand(source.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"{nameof(UserProfile)} bulunamadı: {SampleUserId}");
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Handle: yerel bugün kuralı ----

    [Fact]
    public async Task Handle_WithTargetDateBeforeLocalToday_ThrowsValidationExceptionAndSkipsAdd()
    {
        // Arrange
        var source = CreateSourceTask();
        SetupSource(source);
        var command = CreateCommand(source.Id) with { TargetDate = LocalToday.AddDays(-1) };

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainKey(nameof(CopyTaskToDateCommand.TargetDate))
            .WhoseValue.Should().Contain("Hedef gün bugünden önce olamaz.");
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Europe/Istanbul", 8)]  // 21:30 UTC → İstanbul'da yerel gün 2026-01-08
    [InlineData("America/New_York", 7)] // 21:30 UTC → New York'ta yerel gün 2026-01-07
    [InlineData("America/New_York", 8)]
    public async Task Handle_WithTargetDateOnOrAfterLocalToday_InUserTimeZone_AddsCopy(string timeZoneId, int targetDay)
    {
        // Arrange
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 7, 21, 30, 0, TimeSpan.Zero));
        SetupProfile(timeZoneId);
        var source = CreateSourceTask();
        SetupSource(source);
        var command = CreateCommand(source.Id) with { TargetDate = new DateOnly(2026, 1, targetDay) };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _taskRepository.Verify(
            repository => repository.AddAsync(
                It.Is<PlanningTask>(task => task.AssignedDate == new DateOnly(2026, 1, targetDay)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("Europe/Istanbul", 7)]  // yerel gün 2026-01-08 iken 01-07 geçmişte kalır
    [InlineData("America/New_York", 6)] // yerel gün 2026-01-07 iken 01-06 geçmişte kalır
    public async Task Handle_WithTargetDateBeforeLocalToday_InUserTimeZone_ThrowsValidationException(string timeZoneId, int targetDay)
    {
        // Arrange
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 7, 21, 30, 0, TimeSpan.Zero));
        SetupProfile(timeZoneId);
        var source = CreateSourceTask();
        SetupSource(source);
        var command = CreateCommand(source.Id) with { TargetDate = new DateOnly(2026, 1, targetDay) };

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Handle: gün kilidi ----

    [Fact]
    public async Task Handle_WhenTargetDayAlreadyEvaluated_ThrowsDomainExceptionAndSkipsAdd()
    {
        // Arrange
        var source = CreateSourceTask();
        SetupSource(source);
        SetupStreak(CreateStreakEvaluatedOn(TargetDay)); // hedef günün değerlendirmesi yapılmış
        var command = CreateCommand(source.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage(LockMessage);
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _weekRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningWeek>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Handle: hafta ----

    [Fact]
    public async Task Handle_WhenWeekDoesNotExist_CreatesWeekWithGeneratedIdAndAddsIt()
    {
        // Arrange
        var source = CreateSourceTask();
        SetupSource(source);
        PlanningWeek? addedWeek = null;
        _weekRepository
            .Setup(repository => repository.AddAsync(It.IsAny<PlanningWeek>(), It.IsAny<CancellationToken>()))
            .Callback<PlanningWeek, CancellationToken>((week, _) => addedWeek = week)
            .Returns(Task.CompletedTask);
        var command = CreateCommand(source.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        addedWeek.Should().NotBeNull();
        addedWeek!.Id.Should().NotBe(Guid.Empty);
        addedWeek.UserId.Should().Be(SampleUserId);
        addedWeek.WeekStartDate.Should().Be(TargetWeekStart);
    }

    [Fact]
    public async Task Handle_WhenWeekExists_DoesNotCreateWeekAgain()
    {
        // Arrange
        var source = CreateSourceTask();
        SetupSource(source);
        _weekRepository
            .Setup(repository => repository.GetByStartDateAsync(SampleUserId, TargetWeekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlanningWeek.Create(Guid.NewGuid(), SampleUserId, TargetWeekStart));
        var command = CreateCommand(source.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _weekRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningWeek>(), It.IsAny<CancellationToken>()), Times.Never);
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Handle: idempotency ----

    [Fact]
    public async Task Handle_WhenNewIdAlreadyExists_ReturnsExistingIdWithoutAddingOrQueryingOthers()
    {
        // Arrange
        var command = CreateCommand(Guid.NewGuid());
        var existingCopy = PlanningTask.CreateInPool(
            command.NewId, SampleUserId, "Mevcut kopya", TaskPriority.Normal, SampleSortOrder, SampleCreatedAt);
        _taskRepository
            .Setup(repository => repository.GetByIdIncludingDeletedAsync(SampleUserId, command.NewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCopy);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(command.NewId);
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _taskRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userProfileRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _streakRepository.Verify(
            repository => repository.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExistingCopyIsDeleted_ReturnsExistingIdWithoutAdding()
    {
        // Arrange
        var command = CreateCommand(Guid.NewGuid());
        var deletedCopy = PlanningTask.CreateInPool(
            command.NewId, SampleUserId, "Silinmiş kopya", TaskPriority.Normal, SampleSortOrder, SampleCreatedAt);
        deletedCopy.Delete(CopyCreatedAt);
        _taskRepository
            .Setup(repository => repository.GetByIdIncludingDeletedAsync(SampleUserId, command.NewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedCopy);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(command.NewId);
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Test Helpers ----

    private static CopyTaskToDateCommand CreateCommand(Guid sourceTaskId)
        => new(Guid.NewGuid(), sourceTaskId, TargetDay, OtherSortOrder, CopyCreatedAt);

    private void SetupSource(PlanningTask source)
        => _taskRepository
            .Setup(repository => repository.GetByIdAsync(SampleUserId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

    private void SetupStreak(Streak? streak)
        => _streakRepository
            .Setup(repository => repository.GetByUserIdAsync(SampleUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

    private void SetupProfile(string timeZoneId)
        => _userProfileRepository
            .Setup(repository => repository.GetByIdAsync(SampleUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfile.Create(SampleUserId, timeZoneId, SampleCreatedAt));

    private void CaptureAddedTask(Action<PlanningTask> capture)
        => _taskRepository
            .Setup(repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()))
            .Callback<PlanningTask, CancellationToken>((task, _) => capture(task))
            .Returns(Task.CompletedTask);

    // Kopyalanacak kaynak görev: atanmış, tamamlanmamış, farklı öncelik ve başlıkla
    private static PlanningTask CreateSourceTask(DateOnly? assignedDate = null)
    {
        var task = PlanningTask.CreateInPool(
            Guid.NewGuid(), SampleUserId, "Rapor hazırla", TaskPriority.Critical, SampleSortOrder, SampleCreatedAt);
        task.AssignToDate(assignedDate ?? LocalToday.AddDays(-1), SampleSortOrder);
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
