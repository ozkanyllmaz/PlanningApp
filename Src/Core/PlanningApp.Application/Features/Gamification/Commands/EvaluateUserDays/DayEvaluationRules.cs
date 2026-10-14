using System;

namespace PlanningApp.Application.Features.Gamification.Commands.EvaluateUserDays;

public static class DayEvaluationRules
{
    // Bir gün, kullanıcının yerel saatiyle ertesi gün 06:00'da değerlendirilebilir hale gelir.
    // Amaç: gece yarısına yakın offline yapılıp gecikmeli senkronize edilen tamamlamalar işlensin.
    public static readonly TimeSpan GracePeriod = TimeSpan.FromHours(6);
}
