using System;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Features.Tasks.Dtos;

public sealed record TaskDto(
    Guid Id,
    string Title,
    TaskPriority Priority,
    FocusColor? FocusColor,
    double SortOrder,
    bool IsCompleted,
    DateOnly? AssignedDate,
    TimeOnly? ReminderTime,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt)
{
    // Entity → DTO eşlemesinin tek yeri
    public static TaskDto From(PlanningTask task)
        => new(
            task.Id,
            task.Title,
            task.Priority,
            task.FocusColor,
            task.SortOrder,
            task.IsCompleted,
            task.AssignedDate,
            task.ReminderTime,
            task.CreatedAt,
            task.CompletedAt);
}
