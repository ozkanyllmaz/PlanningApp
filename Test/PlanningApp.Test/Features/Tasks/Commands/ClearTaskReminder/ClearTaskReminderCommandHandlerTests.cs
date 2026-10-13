using FluentAssertions;
using Moq;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Tasks.Commands.ClearTaskReminder;
using PlanningApp.Domain.Common;
using PlanningApp.Domain.Gamification;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.ClearTaskReminder;

public class ClearTaskReminderCommandHandlerTests
{
    // Sabit bir kullanıcı kimliği; testlerin rastgele değerlere bağımlı olmaması için
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");

    // Sabit tarihler; testlerin sistem saatine bağımlı olmaması için (istemci saat dilimi: +03:00)
    private static readonly DateOnly AssignedDay = new(2026, 1, 7);
    private static readonly DateTimeOffset SampleCreatedAt = new(2026, 1, 5, 9, 0, 0, TimeSpan.FromHours(3));

    private const double SampleSortOrder = 1000d;

    private static readonly TimeOnly SampleReminderTime = new(9, 0);

    private readonly Mock<IPlanningTaskRepository> _taskRepository = new();
    private readonly Mock<IStreakRepository> _streakRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ClearTaskReminderCommandHandler _handler;

    public ClearTaskReminderCommandHandlerTests()
    {
        _currentUser.Setup(service => service.UserId).Returns(SampleUserId);
        _handler = new ClearTaskReminderCommandHandler(
            _taskRepository.Object, _streakRepository.Object, _currentUser.Object, _unitOfWork.Object);
    }

    // ---- Handle: başarılı durum ----

    [Fact]
    public async Task Handle_OnTaskWithReminder_ClearsReminderAndSavesChangesOnce()
    {
        // Arrange
        var task = CreateTaskWithReminder();
        SetupTask(task);
        SetupStreak(CreateStreakEvaluatedOn(AssignedDay.AddDays(-1))); // önceki gün değerlendirilmiş
        var command = new ClearTaskReminderCommand(task.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        task.ReminderTime.Should().BeNull();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutStreakRecord_AppliesChange()
    {
        // Arrange
        var task = CreateTaskWithReminder();
        SetupTask(task);
        SetupStreak(null); // kullanıcının henüz streak kaydı yok
        var command = new ClearTaskReminderCommand(task.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        task.ReminderTime.Should().BeNull();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Handle: kullanıcı kimliği ----

    [Fact]
    public async Task Handle_Always_QueriesRepositoriesWithUserIdFromCurrentUserService()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        _currentUser.Setup(service => service.UserId).Returns(currentUserId);
        var task = CreateTaskWithReminder();
        _taskRepository
            .Setup(repository => repository.GetByIdAsync(currentUserId, task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        var command = new ClearTaskReminderCommand(task.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _taskRepository.Verify(
            repository => repository.GetByIdAsync(currentUserId, task.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _streakRepository.Verify(
            repository => repository.GetByUserIdAsync(currentUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Handle: bulunamadı ----

    [Fact]
    public async Task Handle_WhenTaskNotFound_ThrowsNotFoundExceptionAndSkipsSave()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var command = new ClearTaskReminderCommand(taskId); // repository varsayılan olarak null döner

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"{nameof(PlanningTask)} bulunamadı: {taskId}");
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Handle: idempotency ----

    [Fact]
    public async Task Handle_WhenAlreadyInTargetState_ReturnsWithoutSavingOrQueryingStreak()
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay);
        SetupTask(task);
        var command = new ClearTaskReminderCommand(task.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        task.ReminderTime.Should().BeNull();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _streakRepository.Verify(
            repository => repository.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAlreadyInTargetStateAndDayEvaluated_ReturnsWithoutError()
    {
        // Arrange: ilk istek başarılı olmuş, gün sonra değerlendirilmiş; tekrar gelen istek hata vermemeli
        var task = CreateAssignedTask(AssignedDay);
        SetupTask(task);
        SetupStreak(CreateStreakEvaluatedOn(AssignedDay));
        var command = new ClearTaskReminderCommand(task.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Handle: değerlendirilmiş gün ----

    [Theory]
    [InlineData(0)] // atandığı günün kendisi değerlendirilmiş
    [InlineData(2)] // sonraki bir gün değerlendirilmiş
    public async Task Handle_WhenAssignedDayAlreadyEvaluated_ThrowsDomainExceptionAndKeepsTaskUnchanged(int daysAfterAssignedDay)
    {
        // Arrange
        var task = CreateTaskWithReminder();
        SetupTask(task);
        SetupStreak(CreateStreakEvaluatedOn(AssignedDay.AddDays(daysAfterAssignedDay)));
        var command = new ClearTaskReminderCommand(task.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Değerlendirilmiş bir günün görev durumu değiştirilemez.");
        task.ReminderTime.Should().Be(SampleReminderTime);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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

    private static PlanningTask CreateSampleTask()
        => PlanningTask.CreateInPool(
            Guid.NewGuid(), SampleUserId, "Örnek görev", TaskPriority.Normal, SampleSortOrder, SampleCreatedAt);

    private static PlanningTask CreateAssignedTask(DateOnly date)
    {
        var task = CreateSampleTask();
        task.AssignToDate(date, SampleSortOrder);
        return task;
    }

    private static PlanningTask CreateTaskWithReminder()
    {
        var task = CreateAssignedTask(AssignedDay);
        task.SetReminder(SampleReminderTime);
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
