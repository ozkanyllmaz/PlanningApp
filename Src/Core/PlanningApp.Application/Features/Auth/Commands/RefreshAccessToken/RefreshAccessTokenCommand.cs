using MediatR;

namespace PlanningApp.Application.Features.Auth.Commands.RefreshAccessToken;

// Anonim Command: access token süresi dolmuş olabilir, ICurrentUserService kullanılmaz
public sealed record RefreshAccessTokenCommand(string RefreshToken) : IRequest<AuthResult>
{
    // Token log'a düşmesin diye gizlenir
    public override string ToString()
        => "RefreshAccessTokenCommand { RefreshToken = *** }";
}
