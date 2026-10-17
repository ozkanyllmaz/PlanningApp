using MediatR;

namespace PlanningApp.Application.Features.Auth.Queries.GetCurrentUser;

// Girdisi yoktur; kullanıcı kimliği ICurrentUserService'ten alınır
public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;
