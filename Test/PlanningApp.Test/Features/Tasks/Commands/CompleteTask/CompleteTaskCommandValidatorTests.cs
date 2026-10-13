using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using PlanningApp.Application.Features.Tasks.Commands.CompleteTask;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.CompleteTask;

public class CompleteTaskCommandValidatorTests
{
    // Sabit bir an; testlerin sistem saatine bağımlı olmaması için
    private static readonly DateTimeOffset Now = new(2026, 1, 7, 9, 0, 0, TimeSpan.Zero);

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

    // ---- TaskId ----

    [Fact]
    public void Validate_WithEmptyTaskId_IsInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { TaskId = Guid.Empty };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CompleteTaskCommand.TaskId));
    }

    // ---- CompletedAt ----

    [Theory]
    [InlineData(-3600)] // geçmişte
    [InlineData(0)]     // tam şu an
    [InlineData(300)]   // 5 dakika tolerans sınırı
    public void Validate_WithCompletedAtWithinTolerance_IsValid(int secondsFromNow)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { CompletedAt = Now.AddSeconds(secondsFromNow) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(301)]  // toleransın 1 saniye ötesi
    [InlineData(3600)] // 1 saat sonrası
    public void Validate_WithCompletedAtBeyondTolerance_IsInvalid(int secondsFromNow)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { CompletedAt = Now.AddSeconds(secondsFromNow) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(error =>
                error.PropertyName == nameof(CompleteTaskCommand.CompletedAt) &&
                error.ErrorMessage == "Tamamlanma zamanı gelecekte olamaz.");
    }

    // ---- Test Helpers ----

    private static CompleteTaskCommandValidator CreateValidator()
        => new(new FakeTimeProvider(Now));

    private static CompleteTaskCommand CreateSampleCommand()
        => new(Guid.NewGuid(), Now);
}
