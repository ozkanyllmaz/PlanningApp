using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Domain.Users;
using PlanningApp.IntegrationTests.Support;
using PlanningApp.Persistence;

namespace PlanningApp.IntegrationTests.Persistence.Identity;

[Collection(PostgresCollection.Name)]
public class IdentityServiceTests : IDisposable
{
    private const string ValidPassword = "ornek-parola-123";
    private const string WrongPassword = "yanlis-parola-999";
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _fixture;
    private readonly ServiceProvider _provider;

    public IdentityServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _provider = TestServices.BuildPersistenceProvider(TestServices.GetConnectionString(fixture));
    }

    public void Dispose() => _provider.Dispose();

    // ---- CreateUserAsync ----

    [Fact]
    public async Task CreateUserAsync_WithValidValues_Succeeds()
    {
        // Arrange
        var email = NewEmail();
        var userId = Guid.NewGuid();

        // Act
        var result = await WithServiceAsync(service =>
            service.CreateUserAsync(userId, email, ValidPassword, CancellationToken.None));

        // Assert
        result.Succeeded.Should().BeTrue();
        var found = await WithServiceAsync(service => service.FindByIdAsync(userId, CancellationToken.None));
        found.Should().NotBeNull();
        found!.Email.Should().Be(email);
    }

    [Fact]
    public async Task CreateUserAsync_WithSameEmailInDifferentCase_ReturnsEmailError()
    {
        // Arrange
        var email = NewEmail();
        await WithServiceAsync(service =>
            service.CreateUserAsync(Guid.NewGuid(), email, ValidPassword, CancellationToken.None));

        // Act
        var result = await WithServiceAsync(service =>
            service.CreateUserAsync(Guid.NewGuid(), email.ToUpperInvariant(), ValidPassword, CancellationToken.None));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("Email");
        result.Errors["Email"].Should().Contain("Bu e-posta adresi zaten kayıtlı.");
    }

    [Fact]
    public async Task CreateUserAsync_WithShortPassword_ReturnsPasswordError()
    {
        // Arrange
        var email = NewEmail();

        // Act
        var result = await WithServiceAsync(service =>
            service.CreateUserAsync(Guid.NewGuid(), email, "kisa", CancellationToken.None));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("Password");
        result.Errors["Password"].Should().Contain("Parola en az 8 karakter olmalıdır.");
    }

    // ---- CheckCredentialsAsync ----

    [Fact]
    public async Task CheckCredentialsAsync_WithCorrectPassword_ReturnsUserIdAndEmail()
    {
        // Arrange
        var (email, userId) = await CreateUserAsync();

        // Act
        var result = await WithServiceAsync(service =>
            service.CheckCredentialsAsync(email, ValidPassword, CancellationToken.None));

        // Assert
        result.Succeeded.Should().BeTrue();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be(email);
    }

    [Fact]
    public async Task CheckCredentialsAsync_WithWrongPassword_Fails()
    {
        // Arrange
        var (email, _) = await CreateUserAsync();

        // Act
        var result = await WithServiceAsync(service =>
            service.CheckCredentialsAsync(email, WrongPassword, CancellationToken.None));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.Email.Should().BeNull();
    }

    [Fact]
    public async Task CheckCredentialsAsync_WithUnknownEmail_Fails()
    {
        // Arrange & Act
        var result = await WithServiceAsync(service =>
            service.CheckCredentialsAsync(NewEmail(), ValidPassword, CancellationToken.None));

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CheckCredentialsAsync_AfterFiveWrongAttempts_FailsEvenWithCorrectPassword()
    {
        // Arrange
        var (email, _) = await CreateUserAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await WithServiceAsync(service =>
                service.CheckCredentialsAsync(email, WrongPassword, CancellationToken.None));
        }

        // Act
        var result = await WithServiceAsync(service =>
            service.CheckCredentialsAsync(email, ValidPassword, CancellationToken.None));

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CheckCredentialsAsync_AfterSuccessfulLogin_ResetsFailedAttemptCounter()
    {
        // Arrange: 4 hatalı deneme + başarılı giriş + 4 hatalı deneme; sayaç sıfırlanmasaydı hesap kilitlenirdi
        var (email, _) = await CreateUserAsync();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await WithServiceAsync(service =>
                service.CheckCredentialsAsync(email, WrongPassword, CancellationToken.None));
        }

        var firstSuccess = await WithServiceAsync(service =>
            service.CheckCredentialsAsync(email, ValidPassword, CancellationToken.None));

        for (var attempt = 0; attempt < 4; attempt++)
        {
            await WithServiceAsync(service =>
                service.CheckCredentialsAsync(email, WrongPassword, CancellationToken.None));
        }

        // Act
        var secondSuccess = await WithServiceAsync(service =>
            service.CheckCredentialsAsync(email, ValidPassword, CancellationToken.None));

        // Assert
        firstSuccess.Succeeded.Should().BeTrue();
        secondSuccess.Succeeded.Should().BeTrue();
    }

    // ---- FindByIdAsync / DeleteUserAsync ----

    [Fact]
    public async Task FindByIdAsync_WithExistingUser_ReturnsUserInfo()
    {
        // Arrange
        var (email, userId) = await CreateUserAsync();

        // Act
        var result = await WithServiceAsync(service => service.FindByIdAsync(userId, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Email.Should().Be(email);
    }

    [Fact]
    public async Task FindByIdAsync_WithUnknownId_ReturnsNull()
    {
        // Arrange & Act
        var result = await WithServiceAsync(service => service.FindByIdAsync(Guid.NewGuid(), CancellationToken.None));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_WithExistingUser_RemovesUser()
    {
        // Arrange
        var (email, userId) = await CreateUserAsync();

        // Act
        await WithServiceAsync(async service =>
        {
            await service.DeleteUserAsync(userId, CancellationToken.None);
            return true;
        });

        // Assert
        var found = await WithServiceAsync(service => service.FindByIdAsync(userId, CancellationToken.None));
        found.Should().BeNull();
        var check = await WithServiceAsync(service =>
            service.CheckCredentialsAsync(email, ValidPassword, CancellationToken.None));
        check.Succeeded.Should().BeFalse();
    }

    // ---- Telafi senaryosu ----

    // Bu test ChangeTracker.Clear() olmadan KIRILIR: başarısız SaveChanges çakışan profili bağlamda
    // bekleyen (Added) bırakır; Clear() yoksa kullanıcı silme işlemi kendi SaveChanges'inde bu profili
    // yeniden dener ve aynı PK hatasıyla başarısız olur.
    [Fact]
    public async Task DeleteUserAsync_AfterFailedSaveChanges_DeletesUserAndClearsPendingEntities()
    {
        // Arrange: aynı kapsamda kullanıcı oluşturulur, ardından SaveChanges bir PK çakışmasıyla başarısız olur
        var email = NewEmail();
        var userId = Guid.NewGuid();
        var collidingProfileId = Guid.NewGuid();

        // Çakışma kaynağı: başka bir bağlamda önceden kaydedilmiş profil
        await using (var otherContext = _fixture.CreateDbContext())
        {
            otherContext.UserProfiles.Add(UserProfile.Create(collidingProfileId, "Europe/Istanbul", CreatedAt));
            await otherContext.SaveChangesAsync();
        }

        await using var scope = _provider.CreateAsyncScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var context = scope.ServiceProvider.GetRequiredService<PlanningDbContext>();

        var createResult = await identityService.CreateUserAsync(userId, email, ValidPassword, CancellationToken.None);
        createResult.Succeeded.Should().BeTrue();

        // Kayıt akışının bekleyen entity'leri: kullanıcının profili + aynı Id'li (çakışan) ikinci profil
        context.UserProfiles.Add(UserProfile.Create(userId, "Europe/Istanbul", CreatedAt));
        context.UserProfiles.Add(UserProfile.Create(collidingProfileId, "Europe/Istanbul", CreatedAt));

        var saveAct = async () => await context.SaveChangesAsync();
        await saveAct.Should().ThrowAsync<DbUpdateException>();

        // Act
        await identityService.DeleteUserAsync(userId, CancellationToken.None);

        // Assert: kullanıcı gerçekten silindi
        var found = await identityService.FindByIdAsync(userId, CancellationToken.None);
        found.Should().BeNull();

        // Bağlamda bekleyen entity kalmadı
        context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Should().BeEmpty();

        // Bekleyen çakışan profil yeniden denenmez: aynı bağlamda SaveChanges hata fırlatmaz
        var saveAfterDeleteAct = async () => await context.SaveChangesAsync();
        await saveAfterDeleteAct.Should().NotThrowAsync();
    }

    // ---- Test Helpers ----

    // E-posta benzersizliği için her test kendi adresini kullanır
    private static string NewEmail()
        => $"user-{Guid.NewGuid():N}@example.com";

    private async Task<(string Email, Guid UserId)> CreateUserAsync()
    {
        var email = NewEmail();
        var userId = Guid.NewGuid();

        var result = await WithServiceAsync(service =>
            service.CreateUserAsync(userId, email, ValidPassword, CancellationToken.None));
        result.Succeeded.Should().BeTrue();

        return (email, userId);
    }

    // Her işlem kendi scope'unda (kendi DbContext'iyle) çalışır: istek başına scope davranışını taklit eder
    private async Task<T> WithServiceAsync<T>(Func<IIdentityService, Task<T>> action)
    {
        await using var scope = _provider.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<IIdentityService>());
    }
}
