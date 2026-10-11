using FluentAssertions;
using PlanningApp.Domain.Common;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Tasks;

public class PlanningTaskTests
{
    // Sabit bir kullanıcı kimliği; testlerin rastgele değerlere bağımlı olmaması için
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");

    // ---- CreateInPool ----

    [Fact]
    public void CreateInPool_WithValidTitle_ReturnsTaskInPool()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var task = PlanningTask.CreateInPool(id, SampleUserId, "Markette süt al", TaskPriority.Normal, createdAt);

        // Assert
        task.Id.Should().Be(id);
        task.Title.Should().Be("Markette süt al");
        task.AssignedDate.Should().BeNull();
        task.ReminderTime.Should().BeNull();
        task.IsCompleted.Should().BeFalse();
        task.CreatedAt.Should().Be(createdAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateInPool_WithEmptyOrNullTitle_ThrowsDomainException(string? invalidTitle)
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var act = () => PlanningTask.CreateInPool(id, SampleUserId, invalidTitle!, TaskPriority.Normal, createdAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Görev başlığı boş olamaz.");
    }

    [Fact]
    public void CreateInPool_WithSurroundingWhitespace_TrimsTitle()
    {
        // Arrange & Act
        var task = PlanningTask.CreateInPool(Guid.NewGuid(), SampleUserId, "  Spor yap  ", TaskPriority.Normal, DateTimeOffset.UtcNow);

        // Assert
        task.Title.Should().Be("Spor yap");
    }

    [Fact]
    public void CreateInPool_WithGivenPriority_StoresPriority()
    {
        // Arrange
        var priority = TaskPriority.Critical;

        // Act
        var task = PlanningTask.CreateInPool(Guid.NewGuid(), SampleUserId, "Örnek görev", priority, DateTimeOffset.UtcNow);

        // Assert
        task.Priority.Should().Be(TaskPriority.Critical);
    }

    [Fact]
    public void CreateInPool_WithGivenUserId_StoresUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var task = PlanningTask.CreateInPool(Guid.NewGuid(), userId, "Örnek görev", TaskPriority.Normal, DateTimeOffset.UtcNow);

        // Assert
        task.UserId.Should().Be(userId);
    }

    [Fact]
    public void CreateInPool_WithEmptyUserId_ThrowsDomainException()
    {
        // Arrange
        var emptyUserId = Guid.Empty;

        // Act
        var act = () => PlanningTask.CreateInPool(Guid.NewGuid(), emptyUserId, "Örnek görev", TaskPriority.Normal, DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Kullanıcı kimliği boş olamaz.");
    }

    // ---- ChangePriority ----

    [Fact]
    public void ChangePriority_OnIncompleteTask_UpdatesPriority()
    {
        // Arrange
        var task = CreateSampleTask();

        // Act
        task.ChangePriority(TaskPriority.Important);

        // Assert
        task.Priority.Should().Be(TaskPriority.Important);
    }

    [Fact]
    public void ChangePriority_OnCompletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        var act = () => task.ChangePriority(TaskPriority.Important);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmış bir görevin önceliği değiştirilemez.");
    }

    // ---- AssignToDate ----

    [Fact]
    public void AssignToDate_OnPooledTask_SetsAssignedDate()
    {
        // Arrange
        var task = CreateSampleTask();
        var date = DateOnly.FromDateTime(DateTime.Today);

        // Act
        task.AssignToDate(date);

        // Assert
        task.AssignedDate.Should().Be(date);
    }

    [Fact]
    public void AssignToDate_OnCompletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        var act = () => task.AssignToDate(DateOnly.FromDateTime(DateTime.Today));

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmış bir görevin tarihi değiştirilemez.");
    }

    // ---- SetReminder / ClearReminder ----

    [Fact]
    public void SetReminder_OnAssignedTask_SetsReminderTime()
    {
        // Arrange
        var task = CreateSampleTask();
        task.AssignToDate(DateOnly.FromDateTime(DateTime.Today));
        var reminderTime = new TimeOnly(9, 0);

        // Act
        task.SetReminder(reminderTime);

        // Assert
        task.ReminderTime.Should().Be(reminderTime);
    }

    [Fact]
    public void SetReminder_OnPooledTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateSampleTask(); // AssignToDate çağrılmadı, hâlâ havuzda

        // Act
        var act = () => task.SetReminder(new TimeOnly(9, 0));

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Hatırlatıcı sadece bir güne atanmış görevlere eklenebilir.");
    }

    [Fact]
    public void SetReminder_OnCompletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        var act = () => task.SetReminder(new TimeOnly(9, 0));

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmış bir göreve hatırlatıcı eklenemez.");
    }

    [Fact]
    public void ClearReminder_WithExistingReminder_SetsReminderToNull()
    {
        // Arrange
        var task = CreateSampleTask();
        task.AssignToDate(DateOnly.FromDateTime(DateTime.Today));
        task.SetReminder(new TimeOnly(9, 0));

        // Act
        task.ClearReminder();

        // Assert
        task.ReminderTime.Should().BeNull();
    }

    // ---- ReturnToPool ----

    [Fact]
    public void ReturnToPool_OnAssignedTask_ClearsDateAndReminder()
    {
        // Arrange
        var task = CreateSampleTask();
        task.AssignToDate(DateOnly.FromDateTime(DateTime.Today));
        task.SetReminder(new TimeOnly(9, 0));

        // Act
        task.ReturnToPool();

        // Assert
        task.AssignedDate.Should().BeNull();
        task.ReminderTime.Should().BeNull();
    }

    [Fact]
    public void ReturnToPool_OnCompletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        var act = () => task.ReturnToPool();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmış bir görev havuza geri alınamaz.");
    }

    // ---- Complete ----

    [Fact]
    public void Complete_OnAssignedTask_MarksTaskCompleted()
    {
        // Arrange
        var task = CreateSampleTask();
        task.AssignToDate(DateOnly.FromDateTime(DateTime.Today));
        var completedAt = DateTimeOffset.UtcNow;

        // Act
        task.Complete(completedAt);

        // Assert
        task.IsCompleted.Should().BeTrue();
        task.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void Complete_OnPooledTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateSampleTask(); // havuzda, AssignedDate null

        // Act
        var act = () => task.Complete(DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Görev sadece plana atanmışken tamamlanabilir.");
    }

    [Fact]
    public void Complete_OnAlreadyCompletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        var act = () => task.Complete(DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Görev zaten tamamlanmış.");
    }

    // ---- Uncomplete ----

    [Fact]
    public void Uncomplete_OnCompletedTask_RevertsCompletion()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        task.Uncomplete();

        // Assert
        task.IsCompleted.Should().BeFalse();
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Uncomplete_OnIncompleteTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateSampleTask();

        // Act
        var act = () => task.Uncomplete();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmamış bir görev geri alınamaz.");
    }

    // ---- Rename ----

    [Fact]
    public void Rename_OnPooledTask_UpdatesTitle()
    {
        // Arrange
        var task = CreateSampleTask();

        // Act
        task.Rename("Yeni başlık");

        // Assert
        task.Title.Should().Be("Yeni başlık");
    }

    [Fact]
    public void Rename_OnCompletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        var act = () => task.Rename("Yeni başlık");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmış bir görevin başlığı değiştirilemez.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmptyTitle_ThrowsDomainException(string invalidTitle)
    {
        // Arrange
        var task = CreateSampleTask();

        // Act
        var act = () => task.Rename(invalidTitle);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Görev başlığı boş olamaz.");
    }

    // ---- Test Helpers ----

    private static PlanningTask CreateSampleTask()
        => PlanningTask.CreateInPool(Guid.NewGuid(), SampleUserId, "Örnek görev", TaskPriority.Normal, DateTimeOffset.UtcNow);

    private static PlanningTask CreateCompletedTask()
    {
        var task = CreateSampleTask();
        task.AssignToDate(DateOnly.FromDateTime(DateTime.Today));
        task.Complete(DateTimeOffset.UtcNow);
        return task;
    }
}