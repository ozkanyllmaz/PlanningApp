using System.Globalization;
using FluentAssertions;
using PlanningApp.Domain.Common;
using PlanningApp.Domain.Users;
using Xunit;

namespace PlanningApp.Test.Users;

public class UserProfileTests
{
    // Sabit bir kullanıcı kimliği; testlerin rastgele değerlere bağımlı olmaması için
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");

    // Sabit bir an; testlerin sistem saatine bağımlı olmaması için
    private static readonly DateTimeOffset SampleCreatedAt = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

    private const string InvalidTimeZoneMessage = "Geçerli bir saat dilimi kimliği verilmelidir.";

    // ---- Create ----

    [Fact]
    public void Create_WithValidArguments_ReturnsProfile()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var profile = UserProfile.Create(id, "Europe/Istanbul", SampleCreatedAt);

        // Assert
        profile.Id.Should().Be(id);
        profile.TimeZoneId.Should().Be("Europe/Istanbul");
        profile.CreatedAt.Should().Be(SampleCreatedAt);
    }

    [Fact]
    public void Create_WithEmptyId_ThrowsDomainException()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        var act = () => UserProfile.Create(emptyId, "Europe/Istanbul", SampleCreatedAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Kullanıcı kimliği boş olamaz.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Mars/Olympus")]
    public void Create_WithInvalidTimeZone_ThrowsDomainException(string? invalidTimeZoneId)
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var act = () => UserProfile.Create(id, invalidTimeZoneId!, SampleCreatedAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage(InvalidTimeZoneMessage);
    }

    [Fact]
    public void Create_WithSurroundingWhitespaceInTimeZone_TrimsTimeZoneId()
    {
        // Arrange & Act
        var profile = UserProfile.Create(Guid.NewGuid(), "  Europe/Istanbul  ", SampleCreatedAt);

        // Assert
        profile.TimeZoneId.Should().Be("Europe/Istanbul");
    }

    // ---- ChangeTimeZone ----

    [Fact]
    public void ChangeTimeZone_WithValidTimeZone_UpdatesTimeZoneId()
    {
        // Arrange
        var profile = CreateSampleProfile();

        // Act
        profile.ChangeTimeZone("America/New_York");

        // Assert
        profile.TimeZoneId.Should().Be("America/New_York");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Mars/Olympus")]
    public void ChangeTimeZone_WithInvalidTimeZone_ThrowsDomainExceptionAndKeepsTimeZone(string? invalidTimeZoneId)
    {
        // Arrange
        var profile = CreateSampleProfile();

        // Act
        var act = () => profile.ChangeTimeZone(invalidTimeZoneId!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage(InvalidTimeZoneMessage);
        profile.TimeZoneId.Should().Be("Europe/Istanbul");
    }

    // ---- GetLocalDate ----

    [Theory]
    [InlineData("Europe/Istanbul", "2026-01-07T21:30:00+00:00", "2026-01-08")]   // UTC+3: ertesi güne geçer
    [InlineData("America/New_York", "2026-01-07T03:00:00+00:00", "2026-01-06")]  // UTC-5: bir önceki güne düşer
    public void GetLocalDate_WithInstant_ReturnsDateInUserTimeZone(string timeZoneId, string instantText, string expectedDateText)
    {
        // Arrange
        var profile = UserProfile.Create(Guid.NewGuid(), timeZoneId, SampleCreatedAt);
        var instant = DateTimeOffset.Parse(instantText, CultureInfo.InvariantCulture);
        var expectedDate = DateOnly.Parse(expectedDateText, CultureInfo.InvariantCulture);

        // Act
        var localDate = profile.GetLocalDate(instant);

        // Assert
        localDate.Should().Be(expectedDate);
    }

    // ---- Test Helpers ----

    private static UserProfile CreateSampleProfile()
        => UserProfile.Create(SampleUserId, "Europe/Istanbul", SampleCreatedAt);
}
