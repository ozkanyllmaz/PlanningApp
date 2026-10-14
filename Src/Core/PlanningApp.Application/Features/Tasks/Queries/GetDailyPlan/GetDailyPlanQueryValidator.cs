using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Queries.GetDailyPlan;

public sealed class GetDailyPlanQueryValidator : AbstractValidator<GetDailyPlanQuery>
{
    public GetDailyPlanQueryValidator()
    {
        // default(DateOnly) istemcinin tarihi hiç doldurmadığı anlamına gelir
        RuleFor(query => query.Date)
            .NotEmpty().WithMessage("Tarih boş olamaz.");
    }
}
