namespace PlanningApp.Application.Common.Helpers;

public static class WeekCalendar
{
    // Verilen tarihin haftasının başlangıcını (Pazartesi) döner; Pazar günü bir önceki Pazartesi'ye aittir
    public static DateOnly GetWeekStart(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }
}
