using System;

namespace PlanningApp.Application.Features.Auth.Queries.GetCurrentUser;

// Oturum açmış kullanıcının herkese açık görünümü; parola, hash veya token taşımaz
public sealed record CurrentUserDto(Guid UserId, string Email);
