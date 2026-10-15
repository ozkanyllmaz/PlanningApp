using System;

namespace PlanningApp.Application.Common.Models;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt)
{
    // Token log'a düşmesin diye gizlenir
    public override string ToString()
        => $"AccessTokenResult {{ Token = ***, ExpiresAt = {ExpiresAt:O} }}";
}
