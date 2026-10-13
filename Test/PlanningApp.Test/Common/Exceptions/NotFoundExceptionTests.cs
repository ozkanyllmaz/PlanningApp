using FluentAssertions;
using PlanningApp.Application.Common.Exceptions;
using Xunit;

namespace PlanningApp.Test.Common.Exceptions;

public class NotFoundExceptionTests
{
    // ---- Constructor ----

    [Fact]
    public void Constructor_WithEntityNameAndKey_BuildsMessageContainingBoth()
    {
        // Arrange
        var key = new Guid("22222222-2222-2222-2222-222222222222");

        // Act
        var exception = new NotFoundException("PlanningTask", key);

        // Assert
        exception.Message.Should().Be($"PlanningTask bulunamadı: {key}");
        exception.EntityName.Should().Be("PlanningTask");
        exception.Key.Should().Be(key);
    }

    [Fact]
    public void Constructor_WithNonGuidKey_UsesKeyTextInMessage()
    {
        // Arrange & Act
        var exception = new NotFoundException("Streak", 42);

        // Assert
        exception.Message.Should().Be("Streak bulunamadı: 42");
    }
}
