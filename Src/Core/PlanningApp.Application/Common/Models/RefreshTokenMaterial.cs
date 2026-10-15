using System;

namespace PlanningApp.Application.Common.Models;

// PlainToken yalnızca üretildiği yanıtta bulunur; veritabanına yalnızca TokenHash yazılır
public sealed record RefreshTokenMaterial(string PlainToken, string TokenHash, DateTimeOffset ExpiresAt)
{
    // Token ve özeti log'a düşmesin diye gizlenir
    public override string ToString()
        => $"RefreshTokenMaterial {{ PlainToken = ***, TokenHash = ***, ExpiresAt = {ExpiresAt:O} }}";
}
