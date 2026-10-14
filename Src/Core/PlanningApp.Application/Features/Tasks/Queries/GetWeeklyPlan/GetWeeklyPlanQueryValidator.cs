using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Queries.GetWeeklyPlan;

public sealed class GetWeeklyPlanQueryValidator : AbstractValidator<GetWeeklyPlanQuery>
{
    public GetWeeklyPlanQueryValidator()
    {
        // default(DateOnly) istemcinin tarihi hiç doldurmadığı anlamına gelir
        RuleFor(query => query.Date)
            .NotEmpty().WithMessage("Tarih boş olamaz.");
    }
}
