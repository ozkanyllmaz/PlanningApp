using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Queries.GetDailyPlan;

public sealed record GetDailyPlanQuery(DateOnly Date) : IRequest<DailyPlanDto>;
