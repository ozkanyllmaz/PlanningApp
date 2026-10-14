using System;
using MediatR;

namespace PlanningApp.Application.Features.Gamification.Commands.EvaluateUserDays;

// Sistem Command'ı: oturum yoktur, UserId arka plan servisinden gelir.
// Hiçbir HTTP endpoint'ine açılmaz (bkz. cqrs_rules.md → Kullanıcı Kimliği ve Sahiplik).
public sealed record EvaluateUserDaysCommand(Guid UserId) : IRequest<EvaluateUserDaysResult>;
