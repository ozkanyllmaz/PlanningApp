using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using PlanningApp.Application.Features.Tasks.Commands.CreateTask;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandValidatorTests
{
    // Sabit bir an; testlerin sistem saatine bağımlı olmaması için
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

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

    // ---- Id ----

    [Fact]
    public void Validate_WithEmptyId_IsInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Id = Guid.Empty };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateTaskCommand.Id));
    }

    // ---- Title ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullTitle_IsInvalid(string? invalidTitle)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Title = invalidTitle! };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().OnlyContain(error => error.PropertyName == nameof(CreateTaskCommand.Title));
    }

    [Fact]
    public void Validate_WithTitleOfMaxLength_IsValid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Title = new string('a', 200) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithTitleLongerThan200Characters_IsInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Title = new string('a', 201) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateTaskCommand.Title));
    }

    // ---- Priority ----

    [Theory]
    [InlineData(TaskPriority.Normal)]
    [InlineData(TaskPriority.Important)]
    [InlineData(TaskPriority.Critical)]
    public void Validate_WithDefinedPriority_IsValid(TaskPriority priority)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Priority = priority };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Validate_WithUndefinedPriority_IsInvalid(int invalidPriority)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Priority = (TaskPriority)invalidPriority };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateTaskCommand.Priority));
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
                error.PropertyName == nameof(CreateTaskCommand.CreatedAt) &&
                error.ErrorMessage == "Oluşturulma zamanı gelecekte olamaz.");
    }

    // ---- Test Helpers ----

    private static CreateTaskCommandValidator CreateValidator()
        => new(new FakeTimeProvider(Now));

    private static CreateTaskCommand CreateSampleCommand()
        => new(Guid.NewGuid(), "Örnek görev", TaskPriority.Normal, Now);
}
