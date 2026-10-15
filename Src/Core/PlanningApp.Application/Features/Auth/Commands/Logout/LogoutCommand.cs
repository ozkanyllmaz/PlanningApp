using MediatR;

namespace PlanningApp.Application.Features.Auth.Commands.Logout;

// Anonim Command: çıkış, süresi dolmuş access token'la da yapılabilmelidir
public sealed record LogoutCommand(string RefreshToken) : IRequest
{
    // Token log'a düşmesin diye gizlenir
    public override string ToString()
        => "LogoutCommand { RefreshToken = *** }";
}
