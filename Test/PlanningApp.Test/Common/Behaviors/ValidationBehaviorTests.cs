using FluentAssertions;
using FluentValidation;
using MediatR;
using PlanningApp.Application.Common.Behaviors;
using Xunit;
using ValidationException = PlanningApp.Application.Common.Exceptions.ValidationException;

namespace PlanningApp.Test.Common.Behaviors;

public class ValidationBehaviorTests
{
    // ---- Handle: doğrulama hatası ----

    [Fact]
    public async Task Handle_WithValidationFailure_ThrowsValidationExceptionAndSkipsNext()
    {
        // Arrange
        var behavior = CreateBehavior(new SampleRequestValidator());
        var nextCalled = false;

        // Act
        var act = () => behavior.Handle(
            new SampleRequest(""),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult("sonuç");
            },
            CancellationToken.None);

        // Assert
        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainKey(nameof(SampleRequest.Name))
            .WhoseValue.Should().Contain("Ad boş olamaz.");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithFailuresFromMultipleValidators_CollectsAllErrors()
    {
        // Arrange
        var behavior = CreateBehavior(new SampleRequestValidator(), new SampleRequestMaxLengthValidator());

        // Act
        var act = () => behavior.Handle(
            new SampleRequest(""),
            _ => Task.FromResult("sonuç"),
            CancellationToken.None);

        // Assert
        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors[nameof(SampleRequest.Name)].Should()
            .Contain(["Ad boş olamaz.", "Ad en az 3 karakter olmalıdır."]);
    }

    // ---- Handle: hata yok / validator yok ----

    [Fact]
    public async Task Handle_WithValidRequest_CallsNextAndReturnsResponse()
    {
        // Arrange
        var behavior = CreateBehavior(new SampleRequestValidator());
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            new SampleRequest("Geçerli ad"),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult("sonuç");
            },
            CancellationToken.None);

        // Assert
        result.Should().Be("sonuç");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithoutValidators_CallsNextAndReturnsResponse()
    {
        // Arrange
        var behavior = CreateBehavior();
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            new SampleRequest(""),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult("sonuç");
            },
            CancellationToken.None);

        // Assert
        result.Should().Be("sonuç");
        nextCalled.Should().BeTrue();
    }

    // ---- Test Helpers ----

    private static ValidationBehavior<SampleRequest, string> CreateBehavior(
        params IValidator<SampleRequest>[] validators)
        => new(validators);

    private sealed record SampleRequest(string Name) : IRequest<string>;

    private sealed class SampleRequestValidator : AbstractValidator<SampleRequest>
    {
        public SampleRequestValidator()
        {
            RuleFor(request => request.Name).NotEmpty().WithMessage("Ad boş olamaz.");
        }
    }

    private sealed class SampleRequestMaxLengthValidator : AbstractValidator<SampleRequest>
    {
        public SampleRequestMaxLengthValidator()
        {
            RuleFor(request => request.Name).MinimumLength(3).WithMessage("Ad en az 3 karakter olmalıdır.");
        }
    }
}
