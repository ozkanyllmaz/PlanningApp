using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using PlanningApp.Application.Features.Tasks.Commands.CopyTaskToDate;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.CopyTaskToDate;

public class CopyTaskToDateCommandValidatorTests
{
    // Sabit bir an; testlerin sistem saatine bağımlı olmaması için
    private static readonly DateTimeOffset Now = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);

    // ---- Geçerli komut ----

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ---- NewId / SourceTaskId ----

    [Fact]
    public void Validate_WithEmptyNewId_IsInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { NewId = Guid.Empty };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CopyTaskToDateCommand.NewId));
    }

    [Fact]
    public void Validate_WithEmptySourceTaskId_IsInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { SourceTaskId = Guid.Empty };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CopyTaskToDateCommand.SourceTaskId));
    }

    // ---- TargetDate ----

    [Fact]
    public void Validate_WithDefaultTargetDate_IsInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { TargetDate = default };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CopyTaskToDateCommand.TargetDate));
    }

    // ---- SortOrder ----

    [Theory]
    [InlineData(0, true)]
    [InlineData(1000.5, true)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    [InlineData(double.NegativeInfinity, false)]
    public void Validate_WithSortOrder_ReturnsExpectedValidity(double sortOrder, bool expectedValid)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { SortOrder = sortOrder };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Errors.Should().ContainSingle()
                .Which.Should().Match<FluentValidation.Results.ValidationFailure>(error =>
                    error.PropertyName == nameof(CopyTaskToDateCommand.SortOrder) &&
                    error.ErrorMessage == "Sıralama değeri geçerli bir sayı olmalıdır.");
        }
    }

    // ---- CreatedAt ----

    [Theory]
    [InlineData(-3600)] // geçmişte
    [InlineData(0)]     // tam şu an
    [InlineData(300)]   // 5 dakika tolerans sınırı
    public void Validate_WithCreatedAtWithinTolerance_IsValid(int secondsFromNow)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { CreatedAt = Now.AddSeconds(secondsFromNow) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(301)]  // toleransın 1 saniye ötesi
    [InlineData(3600)] // 1 saat sonrası
    public void Validate_WithCreatedAtBeyondTolerance_IsInvalid(int secondsFromNow)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { CreatedAt = Now.AddSeconds(secondsFromNow) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(error =>
                error.PropertyName == nameof(CopyTaskToDateCommand.CreatedAt) &&
                error.ErrorMessage == "Oluşturulma zamanı gelecekte olamaz.");
    }

    // ---- Test Helpers ----

    private static CopyTaskToDateCommandValidator CreateValidator()
        => new(new FakeTimeProvider(Now));

    private static CopyTaskToDateCommand CreateSampleCommand()
        => new(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 9), 1000d, Now);
}
