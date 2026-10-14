using FluentAssertions;
using PlanningApp.Domain.Common;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Tasks;

public class PlanningTaskTests
{
    // Sabit bir kullanıcı kimliği; testlerin rastgele değerlere bağımlı olmaması için
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");

    // Sabit tarihler; testlerin sistem saatine bağımlı olmaması için (istemci saat dilimi: +03:00)
    private static readonly DateOnly AssignedDay = new(2026, 1, 7);
    private static readonly DateTimeOffset SampleCreatedAt = new(2026, 1, 5, 9, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset SampleCompletedAt = new(2026, 1, 7, 10, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset SampleDeletedAt = new(2026, 1, 7, 12, 0, 0, TimeSpan.FromHours(3));

    // Sabit sıralama değerleri; SampleSortOrder yerleştirmelerde, OtherSortOrder değişiklik testlerinde kullanılır
    private const double SampleSortOrder = 1000d;
    private const double OtherSortOrder = 2500.5d;

    private const string InvalidSortOrderMessage = "Sıralama değeri geçerli bir sayı olmalıdır.";
    private const string DeletedTaskMessage = "Silinmiş bir görev değiştirilemez.";

    // Geçersiz (sonlu olmayan) sıralama değerleri
    public static TheoryData<double> NonFiniteSortOrders => new()
    {
        double.NaN,
        double.PositiveInfinity,
        double.NegativeInfinity
    };

    // Silinmiş bir görev üzerinde denenen tüm değiştirici işlemler (CreateDeletedTask() ile hazırlanan görev)
    public static TheoryData<string, Action<PlanningTask>> DeletedTaskOperations => new()
    {
        { "Rename", task => task.Rename("Yeni başlık") },
        { "ChangePriority", task => task.ChangePriority(TaskPriority.Important) },
        { "SetFocusColor", task => task.SetFocusColor(FocusColor.Red) },
        { "Reorder", task => task.Reorder(OtherSortOrder) },
        { "AssignToDate", task => task.AssignToDate(AssignedDay.AddDays(1), OtherSortOrder) },
        { "ReturnToPool", task => task.ReturnToPool(OtherSortOrder) },
        { "SetReminder", task => task.SetReminder(new TimeOnly(9, 0)) },
        { "ClearReminder", task => task.ClearReminder() },
        { "Complete", task => task.Complete(SampleCompletedAt) },
        { "Uncomplete", task => task.Uncomplete() },
        { "CopyToDate", task => task.CopyToDate(Guid.NewGuid(), AssignedDay.AddDays(1), OtherSortOrder, SampleCreatedAt) }
    };

    // ---- CreateInPool ----

    [Fact]
    public void CreateInPool_WithValidTitle_ReturnsTaskInPool()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = SampleCreatedAt;

        // Act
        var task = PlanningTask.CreateInPool(id, SampleUserId, "Markette süt al", TaskPriority.Normal, SampleSortOrder, createdAt);

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
        var createdAt = SampleCreatedAt;

        // Act
        var act = () => PlanningTask.CreateInPool(id, SampleUserId, invalidTitle!, TaskPriority.Normal, SampleSortOrder, createdAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Görev başlığı boş olamaz.");
    }

    [Fact]
    public void CreateInPool_WithSurroundingWhitespace_TrimsTitle()
    {
        // Arrange & Act
        var task = PlanningTask.CreateInPool(Guid.NewGuid(), SampleUserId, "  Spor yap  ", TaskPriority.Normal, SampleSortOrder, SampleCreatedAt);

        // Assert
        task.Title.Should().Be("Spor yap");
    }

    [Fact]
    public void CreateInPool_WithGivenPriority_StoresPriority()
    {
        // Arrange
        var priority = TaskPriority.Critical;

        // Act
        var task = PlanningTask.CreateInPool(Guid.NewGuid(), SampleUserId, "Örnek görev", priority, SampleSortOrder, SampleCreatedAt);

        // Assert
        task.Priority.Should().Be(TaskPriority.Critical);
    }

    [Fact]
    public void CreateInPool_WithGivenUserId_StoresUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var task = PlanningTask.CreateInPool(Guid.NewGuid(), userId, "Örnek görev", TaskPriority.Normal, SampleSortOrder, SampleCreatedAt);

        // Assert
        task.UserId.Should().Be(userId);
    }

    [Fact]
    public void CreateInPool_WithEmptyUserId_ThrowsDomainException()
    {
        // Arrange
        var emptyUserId = Guid.Empty;

        // Act
        var act = () => PlanningTask.CreateInPool(Guid.NewGuid(), emptyUserId, "Örnek görev", TaskPriority.Normal, SampleSortOrder, SampleCreatedAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Kullanıcı kimliği boş olamaz.");
    }

    [Fact]
    public void CreateInPool_WithGivenSortOrder_StoresSortOrder()
    {
        // Arrange
        var sortOrder = 1500.5d;

        // Act
        var task = PlanningTask.CreateInPool(Guid.NewGuid(), SampleUserId, "Örnek görev", TaskPriority.Normal, sortOrder, SampleCreatedAt);

        // Assert
        task.SortOrder.Should().Be(sortOrder);
    }

    [Theory]
    [MemberData(nameof(NonFiniteSortOrders))]
    public void CreateInPool_WithNonFiniteSortOrder_ThrowsDomainException(double invalidSortOrder)
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var act = () => PlanningTask.CreateInPool(id, SampleUserId, "Örnek görev", TaskPriority.Normal, invalidSortOrder, SampleCreatedAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage(InvalidSortOrderMessage);
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

    // ---- SetFocusColor ----

    [Fact]
    public void SetFocusColor_WithColor_SetsFocusColor()
    {
        // Arrange
        var task = CreateSampleTask();

        // Act
        task.SetFocusColor(FocusColor.Purple);

        // Assert
        task.FocusColor.Should().Be(FocusColor.Purple);
    }

    [Fact]
    public void SetFocusColor_WithNull_RemovesFocusColor()
    {
        // Arrange
        var task = CreateSampleTask();
        task.SetFocusColor(FocusColor.Blue);

        // Act
        task.SetFocusColor(null);

        // Assert
        task.FocusColor.Should().BeNull();
    }

    [Fact]
    public void SetFocusColor_WithUndefinedValue_ThrowsDomainException()
    {
        // Arrange
        var task = CreateSampleTask();
        task.SetFocusColor(FocusColor.Green);

        // Act
        var act = () => task.SetFocusColor((FocusColor)99);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Geçersiz odak rengi.");
        task.FocusColor.Should().Be(FocusColor.Green);
    }

    [Fact]
    public void SetFocusColor_OnCompletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        var act = () => task.SetFocusColor(FocusColor.Red);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmış bir görevin odak rengi değiştirilemez.");
    }

    // ---- Reorder ----

    [Fact]
    public void Reorder_OnIncompleteTask_UpdatesSortOrder()
    {
        // Arrange
        var task = CreateSampleTask();

        // Act
        task.Reorder(OtherSortOrder);

        // Assert
        task.SortOrder.Should().Be(OtherSortOrder);
    }

    [Fact]
    public void Reorder_OnCompletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        var act = () => task.Reorder(OtherSortOrder);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmış bir görev yeniden sıralanamaz.");
        task.SortOrder.Should().Be(SampleSortOrder);
    }

    [Theory]
    [MemberData(nameof(NonFiniteSortOrders))]
    public void Reorder_WithNonFiniteSortOrder_ThrowsDomainException(double invalidSortOrder)
    {
        // Arrange
        var task = CreateSampleTask();

        // Act
        var act = () => task.Reorder(invalidSortOrder);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage(InvalidSortOrderMessage);
        task.SortOrder.Should().Be(SampleSortOrder);
    }

    // ---- AssignToDate ----

    [Fact]
    public void AssignToDate_OnPooledTask_SetsAssignedDate()
    {
        // Arrange
        var task = CreateSampleTask();
        var date = AssignedDay;

        // Act
        task.AssignToDate(date, SampleSortOrder);

        // Assert
        task.AssignedDate.Should().Be(date);
    }

    [Fact]
    public void AssignToDate_OnCompletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        var act = () => task.AssignToDate(AssignedDay, SampleSortOrder);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmış bir görevin tarihi değiştirilemez.");
    }

    [Fact]
    public void AssignToDate_WithSortOrder_UpdatesSortOrder()
    {
        // Arrange
        var task = CreateSampleTask();

        // Act
        task.AssignToDate(AssignedDay, OtherSortOrder);

        // Assert
        task.AssignedDate.Should().Be(AssignedDay);
        task.SortOrder.Should().Be(OtherSortOrder);
    }

    [Theory]
    [MemberData(nameof(NonFiniteSortOrders))]
    public void AssignToDate_WithNonFiniteSortOrder_ThrowsDomainException(double invalidSortOrder)
    {
        // Arrange
        var task = CreateSampleTask();

        // Act
        var act = () => task.AssignToDate(AssignedDay, invalidSortOrder);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage(InvalidSortOrderMessage);
        task.AssignedDate.Should().BeNull();
        task.SortOrder.Should().Be(SampleSortOrder);
    }

    // ---- SetReminder / ClearReminder ----

    [Fact]
    public void SetReminder_OnAssignedTask_SetsReminderTime()
    {
        // Arrange
        var task = CreateSampleTask();
        task.AssignToDate(AssignedDay, SampleSortOrder);
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
        task.AssignToDate(AssignedDay, SampleSortOrder);
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
        task.AssignToDate(AssignedDay, SampleSortOrder);
        task.SetReminder(new TimeOnly(9, 0));

        // Act
        task.ReturnToPool(SampleSortOrder);

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
        var act = () => task.ReturnToPool(SampleSortOrder);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmış bir görev havuza geri alınamaz.");
    }

    [Fact]
    public void ReturnToPool_WithSortOrder_UpdatesSortOrder()
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay);

        // Act
        task.ReturnToPool(OtherSortOrder);

        // Assert
        task.AssignedDate.Should().BeNull();
        task.SortOrder.Should().Be(OtherSortOrder);
    }

    [Theory]
    [MemberData(nameof(NonFiniteSortOrders))]
    public void ReturnToPool_WithNonFiniteSortOrder_ThrowsDomainException(double invalidSortOrder)
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay);

        // Act
        var act = () => task.ReturnToPool(invalidSortOrder);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage(InvalidSortOrderMessage);
        task.AssignedDate.Should().Be(AssignedDay);
        task.SortOrder.Should().Be(SampleSortOrder);
    }

    // ---- Complete ----

    [Fact]
    public void Complete_OnAssignedTask_MarksTaskCompleted()
    {
        // Arrange
        var task = CreateSampleTask();
        task.AssignToDate(AssignedDay, SampleSortOrder);
        var completedAt = SampleCompletedAt;

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
        var act = () => task.Complete(SampleCompletedAt);

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
        var act = () => task.Complete(SampleCompletedAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Görev zaten tamamlanmış.");
    }

    // ---- Complete: tamamlama günü kuralı ----

    [Theory]
    [InlineData(0, 0, 0)]    // atandığı günün ilk saniyesi
    [InlineData(10, 0, 0)]   // gün ortası
    [InlineData(23, 59, 59)] // atandığı günün son saniyesi
    public void Complete_OnAssignedDate_MarksTaskCompleted(int hour, int minute, int second)
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay);
        var completedAt = new DateTimeOffset(2026, 1, 7, hour, minute, second, TimeSpan.FromHours(3));

        // Act
        task.Complete(completedAt);

        // Assert
        task.IsCompleted.Should().BeTrue();
        task.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void Complete_WhenLocalDateMatchesButUtcDateDiffers_MarksTaskCompleted()
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay);
        var completedAt = new DateTimeOffset(2026, 1, 7, 1, 0, 0, TimeSpan.FromHours(3)); // UTC karşılığı: 2026-01-06 22:00

        // Act
        task.Complete(completedAt);

        // Assert
        completedAt.UtcDateTime.Date.Should().Be(new DateTime(2026, 1, 6)); // UTC günü gerçekten farklı
        task.IsCompleted.Should().BeTrue();
        task.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void Complete_OnTaskAssignedToFutureDate_ThrowsDomainException()
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay.AddDays(1)); // yarına atanmış

        // Act
        var act = () => task.Complete(SampleCompletedAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Gelecekteki bir güne atanmış görev tamamlanamaz.");
        task.IsCompleted.Should().BeFalse();
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Complete_OnTaskAssignedToPastDate_ThrowsDomainException()
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay.AddDays(-1)); // düne atanmış

        // Act
        var act = () => task.Complete(SampleCompletedAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Geçmiş güne atanmış görev tamamlanamaz; görevi yeni bir güne kopyalayın.");
        task.IsCompleted.Should().BeFalse();
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Complete_AfterPullingFutureTaskToCompletionDate_MarksTaskCompleted()
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay.AddDays(1)); // yarına atanmış
        task.AssignToDate(AssignedDay, SampleSortOrder); // kullanıcı görevi bugüne çekti

        // Act
        task.Complete(SampleCompletedAt);

        // Assert
        task.IsCompleted.Should().BeTrue();
        task.CompletedAt.Should().Be(SampleCompletedAt);
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

    [Fact]
    public void Complete_OnAssignedTask_DoesNotChangeSortOrder()
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay);
        var sortOrderBefore = task.SortOrder;

        // Act
        task.Complete(SampleCompletedAt);

        // Assert
        task.SortOrder.Should().Be(sortOrderBefore);
    }

    [Fact]
    public void Uncomplete_AfterComplete_RestoresSameSortOrder()
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay);
        var sortOrderBefore = task.SortOrder;
        task.Complete(SampleCompletedAt);

        // Act
        task.Uncomplete();

        // Assert
        task.SortOrder.Should().Be(sortOrderBefore);
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

    // ---- CopyToDate ----

    [Fact]
    public void CopyToDate_OnIncompleteTask_ReturnsIncompleteCopyAssignedToTargetDate()
    {
        // Arrange
        var source = PlanningTask.CreateInPool(
            Guid.NewGuid(), SampleUserId, "Rapor hazırla", TaskPriority.Critical, SampleSortOrder, SampleCreatedAt);
        source.AssignToDate(AssignedDay.AddDays(-1), SampleSortOrder); // geçmiş güne atanmış, tamamlanmamış
        var newId = Guid.NewGuid();
        var targetDate = AssignedDay;
        var copyCreatedAt = SampleCompletedAt;

        // Act
        var copy = source.CopyToDate(newId, targetDate, SampleSortOrder, copyCreatedAt);

        // Assert
        copy.Id.Should().Be(newId);
        copy.Id.Should().NotBe(source.Id);
        copy.UserId.Should().Be(source.UserId);
        copy.Title.Should().Be("Rapor hazırla");
        copy.Priority.Should().Be(TaskPriority.Critical);
        copy.AssignedDate.Should().Be(targetDate);
        copy.IsCompleted.Should().BeFalse();
        copy.CompletedAt.Should().BeNull();
        copy.ReminderTime.Should().BeNull();
        copy.CreatedAt.Should().Be(copyCreatedAt);
    }

    [Fact]
    public void CopyToDate_OnTaskWithReminder_DoesNotCopyReminderAndLeavesSourceUnchanged()
    {
        // Arrange
        var source = CreateSampleTask();
        var sourceDate = AssignedDay.AddDays(-1);
        var reminderTime = new TimeOnly(9, 0);
        source.AssignToDate(sourceDate, SampleSortOrder);
        source.SetReminder(reminderTime);

        // Act
        var copy = source.CopyToDate(Guid.NewGuid(), AssignedDay, SampleSortOrder, SampleCompletedAt);

        // Assert
        copy.ReminderTime.Should().BeNull();
        source.AssignedDate.Should().Be(sourceDate);
        source.ReminderTime.Should().Be(reminderTime);
        source.IsCompleted.Should().BeFalse();
        source.Title.Should().Be("Örnek görev");
        source.Priority.Should().Be(TaskPriority.Normal);
    }

    [Fact]
    public void CopyToDate_OnCompletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        var act = () => task.CopyToDate(Guid.NewGuid(), AssignedDay.AddDays(1), SampleSortOrder, SampleCompletedAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Tamamlanmış bir görev kopyalanamaz.");
    }

    [Fact]
    public void CopyToDate_OnTaskWithFocusColor_CopiesFocusColor()
    {
        // Arrange
        var source = CreateAssignedTask(AssignedDay.AddDays(-1));
        source.SetFocusColor(FocusColor.Orange);

        // Act
        var copy = source.CopyToDate(Guid.NewGuid(), AssignedDay, SampleSortOrder, SampleCompletedAt);

        // Assert
        copy.FocusColor.Should().Be(FocusColor.Orange);
        source.FocusColor.Should().Be(FocusColor.Orange);
    }

    [Fact]
    public void CopyToDate_WithGivenSortOrder_UsesGivenSortOrderForCopy()
    {
        // Arrange
        var source = CreateAssignedTask(AssignedDay.AddDays(-1)); // SortOrder: SampleSortOrder

        // Act
        var copy = source.CopyToDate(Guid.NewGuid(), AssignedDay, OtherSortOrder, SampleCompletedAt);

        // Assert
        copy.SortOrder.Should().Be(OtherSortOrder);
        source.SortOrder.Should().Be(SampleSortOrder);
    }

    [Theory]
    [MemberData(nameof(NonFiniteSortOrders))]
    public void CopyToDate_WithNonFiniteSortOrder_ThrowsDomainException(double invalidSortOrder)
    {
        // Arrange
        var source = CreateAssignedTask(AssignedDay.AddDays(-1));

        // Act
        var act = () => source.CopyToDate(Guid.NewGuid(), AssignedDay, invalidSortOrder, SampleCompletedAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage(InvalidSortOrderMessage);
    }

    // ---- Delete ----

    [Fact]
    public void Delete_OnActiveTask_SetsDeletedFlags()
    {
        // Arrange
        var task = CreateAssignedTask(AssignedDay);

        // Act
        task.Delete(SampleDeletedAt);

        // Assert
        task.IsDeleted.Should().BeTrue();
        task.DeletedAt.Should().Be(SampleDeletedAt);
    }

    [Fact]
    public void Delete_OnAlreadyDeletedTask_ThrowsDomainException()
    {
        // Arrange
        var task = CreateDeletedTask();

        // Act
        var act = () => task.Delete(SampleDeletedAt.AddHours(1));

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Görev zaten silinmiş.");
        task.DeletedAt.Should().Be(SampleDeletedAt);
    }

    [Fact]
    public void Delete_OnCompletedTask_SetsDeletedFlags()
    {
        // Arrange
        var task = CreateCompletedTask();

        // Act
        task.Delete(SampleDeletedAt);

        // Assert
        task.IsDeleted.Should().BeTrue();
        task.DeletedAt.Should().Be(SampleDeletedAt);
        task.IsCompleted.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(DeletedTaskOperations))]
    public void Operation_OnDeletedTask_ThrowsDomainException(string operationName, Action<PlanningTask> operation)
    {
        // Arrange
        var task = CreateDeletedTask();

        // Act
        var act = () => operation(task);

        // Assert
        act.Should().Throw<DomainException>(because: operationName)
            .WithMessage(DeletedTaskMessage);
    }

    // ---- Test Helpers ----

    private static PlanningTask CreateSampleTask()
        => PlanningTask.CreateInPool(Guid.NewGuid(), SampleUserId, "Örnek görev", TaskPriority.Normal, SampleSortOrder, SampleCreatedAt);

    private static PlanningTask CreateAssignedTask(DateOnly date)
    {
        var task = CreateSampleTask();
        task.AssignToDate(date, SampleSortOrder);
        return task;
    }

    private static PlanningTask CreateCompletedTask()
    {
        var task = CreateAssignedTask(AssignedDay);
        task.Complete(SampleCompletedAt);
        return task;
    }

    private static PlanningTask CreateDeletedTask()
    {
        var task = CreateAssignedTask(AssignedDay);
        task.Delete(SampleDeletedAt);
        return task;
    }
}
