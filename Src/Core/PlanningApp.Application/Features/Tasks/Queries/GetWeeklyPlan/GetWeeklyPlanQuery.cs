using System;
using System.Collections.Generic;
using MediatR;
using PlanningApp.Application.Features.Tasks.Queries.GetDailyPlan;

namespace PlanningApp.Application.Features.Tasks.Queries.GetWeeklyPlan;

// Date: haftanın herhangi bir günü; hafta Pazartesi başlar
public sealed record GetWeeklyPlanQuery(DateOnly Date) : IRequest<IReadOnlyList<DailyPlanDto>>;
