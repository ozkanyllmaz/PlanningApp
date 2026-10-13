using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using PlanningApp.Application.Features.Tasks.Commands.DeleteTask;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.DeleteTask;

public class DeleteTaskCommandValidatorTests
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
            .Which.PropertyName.Should().Be(nameof(DeleteTaskCommand.TaskId));
    }

    // ---- DeletedAt ----

    [Theory]
    [InlineData(-3600)] // geçmişte
    [InlineData(0)]     // tam şu an
    [InlineData(300)]   // 5 dakika tolerans sınırı
    public void Validate_WithDeletedAtWithinTolerance_IsValid(int secondsFromNow)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { DeletedAt = Now.AddSeconds(secondsFromNow) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(301)]  // toleransın 1 saniye ötesi
    [InlineData(3600)] // 1 saat sonrası
    public void Validate_WithDeletedAtBeyondTolerance_IsInvalid(int secondsFromNow)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { DeletedAt = Now.AddSeconds(secondsFromNow) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(error =>
                error.PropertyName == nameof(DeleteTaskCommand.DeletedAt) &&
                error.ErrorMessage == "Silinme zamanı gelecekte olamaz.");
    }

    // ---- Test Helpers ----

    private static DeleteTaskCommandValidator CreateValidator()
        => new(new FakeTimeProvider(Now));

    private static DeleteTaskCommand CreateSampleCommand()
        => new(Guid.NewGuid(), Now);
}
