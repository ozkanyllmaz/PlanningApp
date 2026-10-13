using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Commands.AssignTaskToDate;

// Date: hedef gün (kullanıcının yerel takvim günü). SortOrder'ı istemci hesaplar.
public sealed record AssignTaskToDateCommand(Guid TaskId, DateOnly Date, double SortOrder) : IRequest;
