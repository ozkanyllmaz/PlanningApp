using FluentAssertions;
using Moq;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Tasks.Commands.CreateTask;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandlerTests
{
    // Sabit bir kullanıcı kimliği; testlerin rastgele değerlere bağımlı olmaması için
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");

    // Sabit bir sıralama değeri; testlerin rastgele değerlere bağımlı olmaması için
    private const double SampleSortOrder = 1000d;

    // Sabit bir an; testlerin sistem saatine bağımlı olmaması için
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

    private readonly Mock<IPlanningTaskRepository> _taskRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateTaskCommandHandler _handler;

    public CreateTaskCommandHandlerTests()
    {
        _currentUser.Setup(service => service.UserId).Returns(SampleUserId);
        _handler = new CreateTaskCommandHandler(_taskRepository.Object, _currentUser.Object, _unitOfWork.Object);
    }

    // ---- Handle: yeni görev ----

    [Fact]
    public async Task Handle_WithNewTask_AddsTaskAndSavesChangesOnce()
    {
        // Arrange
        var command = CreateSampleCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(command.Id);
        _taskRepository.Verify(
            repository => repository.AddAsync(
                It.Is<PlanningTask>(task =>
                    task.Id == command.Id &&
                    task.Title == command.Title &&
                    task.Priority == command.Priority &&
                    task.CreatedAt == command.CreatedAt),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNewTask_UsesUserIdFromCurrentUserService()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        _currentUser.Setup(service => service.UserId).Returns(currentUserId);

        PlanningTask? addedTask = null;
        _taskRepository
            .Setup(repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()))
            .Callback<PlanningTask, CancellationToken>((task, _) => addedTask = task)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(CreateSampleCommand(), CancellationToken.None);

        // Assert
        addedTask.Should().NotBeNull();
        addedTask!.UserId.Should().Be(currentUserId);
    }

    [Fact]
    public async Task Handle_Always_QueriesRepositoryWithUserIdFromCurrentUserService()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        _currentUser.Setup(service => service.UserId).Returns(currentUserId);
        var command = CreateSampleCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _taskRepository.Verify(
            repository => repository.GetByIdAsync(currentUserId, command.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Handle: idempotency ----

    [Fact]
    public async Task Handle_WithExistingId_ReturnsExistingIdWithoutAddingOrSaving()
    {
        // Arrange
        var command = CreateSampleCommand();
        var existingTask = PlanningTask.CreateInPool(
            command.Id, SampleUserId, "Mevcut görev", TaskPriority.Normal, SampleSortOrder, CreatedAt);

        _taskRepository
            .Setup(repository => repository.GetByIdAsync(SampleUserId, command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existingTask.Id);
        _taskRepository.Verify(
            repository => repository.AddAsync(It.IsAny<PlanningTask>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Test Helpers ----

    private static CreateTaskCommand CreateSampleCommand()
        => new(Guid.NewGuid(), "Örnek görev", TaskPriority.Important, SampleSortOrder, CreatedAt);
}
