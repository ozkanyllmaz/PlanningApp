using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Time.Testing;
using PlanningApp.Application.Common.Validation;
using Xunit;

namespace PlanningApp.Test.Common.Validation;

public class ValidatorExtensionsTests
{
    // Sabit bir an; testlerin sistem saatine bağımlı olmaması için
    private static readonly DateTimeOffset Now = new(2026, 1, 7, 9, 0, 0, TimeSpan.Zero);

    private const string FutureMessage = "Zaman gelecekte olamaz.";

    // ---- MustNotBeInFuture ----

    [Theory]
    [InlineData(-3600)] // geçmişte
    [InlineData(0)]     // tam şu an
    [InlineData(300)]   // tam +5 dakika: tolerans sınırı
    public void MustNotBeInFuture_WithinTolerance_IsValid(int secondsFromNow)
    {
        // Arrange
        var validator = CreateValidator();
        var request = new SampleRequest(Now.AddSeconds(secondsFromNow));

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(301)]  // +5 dakika 1 saniye
    [InlineData(3600)] // 1 saat sonrası
    public void MustNotBeInFuture_BeyondTolerance_IsInvalidWithGivenMessage(int secondsFromNow)
    {
        // Arrange
        var validator = CreateValidator();
        var request = new SampleRequest(Now.AddSeconds(secondsFromNow));

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(error =>
                error.PropertyName == nameof(SampleRequest.Timestamp) &&
                error.ErrorMessage == FutureMessage);
    }

    [Theory]
    [InlineData(300, true)]   // +03:00 offset'li zaman damgası, tam +5 dakika
    [InlineData(301, false)]  // +03:00 offset'li zaman damgası, +5 dakika 1 saniye
    public void MustNotBeInFuture_WithNonUtcOffset_ComparesAsAbsoluteInstants(int secondsFromNow, bool expectedValid)
    {
        // Arrange
        var validator = CreateValidator();
        // Aynı an, istemcinin yerel offset'iyle (yerel saat duvar saati olarak 3 saat ileri görünür)
        var timestamp = Now.AddSeconds(secondsFromNow).ToOffset(TimeSpan.FromHours(3));
        var request = new SampleRequest(timestamp);

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Tolerance_Always_IsFiveMinutes()
    {
        // Arrange & Act
        var tolerance = ClockSkew.Tolerance;

        // Assert
        tolerance.Should().Be(TimeSpan.FromMinutes(5));
    }

    // ---- Test Helpers ----

    private sealed record SampleRequest(DateTimeOffset Timestamp);

    private sealed class SampleRequestValidator : AbstractValidator<SampleRequest>
    {
        public SampleRequestValidator(TimeProvider timeProvider)
        {
            RuleFor(request => request.Timestamp).MustNotBeInFuture(timeProvider, FutureMessage);
        }
    }

    private static SampleRequestValidator CreateValidator()
        => new(new FakeTimeProvider(Now));
}
