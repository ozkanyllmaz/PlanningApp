using FluentAssertions;
using PlanningApp.Application.Common.Helpers;
using Xunit;

namespace PlanningApp.Test.Common.Helpers;

public class WeekCalendarTests
{
    // ---- GetWeekStart ----

    [Theory]
    [InlineData(2026, 1, 5, 2026, 1, 5)]    // Pazartesi: kendisi
    [InlineData(2026, 1, 7, 2026, 1, 5)]    // Çarşamba
    [InlineData(2026, 1, 11, 2026, 1, 5)]   // Pazar: aynı haftanın Pazartesi'si (bir sonrakine değil)
    [InlineData(2026, 1, 12, 2026, 1, 12)]  // bir sonraki Pazartesi
    [InlineData(2026, 3, 1, 2026, 2, 23)]   // ay geçişi: Pazar, önceki ayın Pazartesi'si
    [InlineData(2026, 1, 1, 2025, 12, 29)]  // yıl geçişi: Perşembe, önceki yılın Pazartesi'si
    [InlineData(2027, 1, 3, 2026, 12, 28)]  // yıl geçişi: Pazar, önceki yılın Pazartesi'si
    [InlineData(2024, 3, 1, 2024, 2, 26)]   // artık yıl: 29 Şubat'ı içeren hafta
    public void GetWeekStart_WithDate_ReturnsMondayOfThatWeek(
        int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var expected = new DateOnly(expectedYear, expectedMonth, expectedDay);

        // Act
        var weekStart = WeekCalendar.GetWeekStart(date);

        // Assert
        weekStart.Should().Be(expected);
        weekStart.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Theory]
    [InlineData(0)] // Pazartesi
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)] // Pazar
    public void GetWeekStart_ForEveryDayOfSameWeek_ReturnsSameMonday(int daysAfterMonday)
    {
        // Arrange
        var monday = new DateOnly(2026, 1, 5);
        var date = monday.AddDays(daysAfterMonday);

        // Act
        var weekStart = WeekCalendar.GetWeekStart(date);

        // Assert
        weekStart.Should().Be(monday);
    }
}
