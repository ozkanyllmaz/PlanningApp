using MediatR;

namespace PlanningApp.Application.Features.Auth.Commands.Register;

// Anonim Command: oturum yoktur, ICurrentUserService kullanılmaz
public sealed record RegisterCommand(string Email, string Password, string TimeZoneId) : IRequest<AuthResult>
{
    // Parola log'a düşmesin diye gizlenir
    public override string ToString()
        => $"RegisterCommand {{ Email = {Email}, Password = ***, TimeZoneId = {TimeZoneId} }}";
}
