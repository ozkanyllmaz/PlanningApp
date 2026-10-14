using System;
using System.Collections.Generic;
using PlanningApp.Application.Features.Tasks.Dtos;

namespace PlanningApp.Application.Features.Tasks.Queries.GetDailyPlan;

// WeightedCompletionPercentage: günde görev yoksa null
public sealed record DailyPlanDto(
    DateOnly Date,
    bool IsLocked,
    decimal? WeightedCompletionPercentage,
    IReadOnlyList<TaskDto> Tasks);
