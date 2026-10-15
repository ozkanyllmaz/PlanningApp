using System;

namespace PlanningApp.Application.Features.Auth;

public sealed record AuthResult(
    Guid UserId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    // Token'lar log'a düşmesin diye gizlenir
    public override string ToString()
        => $"AuthResult {{ UserId = {UserId}, AccessToken = ***, AccessTokenExpiresAt = {AccessTokenExpiresAt:O}, " +
           $"RefreshToken = ***, RefreshTokenExpiresAt = {RefreshTokenExpiresAt:O} }}";
}
