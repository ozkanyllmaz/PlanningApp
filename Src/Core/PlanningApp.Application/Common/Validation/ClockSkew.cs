namespace PlanningApp.Application.Common.Validation;

public static class ClockSkew
{
    // İstemci saati ile sunucu saati arasındaki küçük farka izin verilen tolerans
    public static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);
}
