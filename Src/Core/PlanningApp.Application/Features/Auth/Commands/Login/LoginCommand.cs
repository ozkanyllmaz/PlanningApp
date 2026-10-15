using MediatR;

namespace PlanningApp.Application.Features.Auth.Commands.Login;

// Anonim Command: oturum yoktur, ICurrentUserService kullanılmaz
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResult>
{
    // Parola log'a düşmesin diye gizlenir
    public override string ToString()
        => $"LoginCommand {{ Email = {Email}, Password = *** }}";
}
