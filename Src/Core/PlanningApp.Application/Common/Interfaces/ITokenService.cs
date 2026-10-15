using System;
using PlanningApp.Application.Common.Models;

namespace PlanningApp.Application.Common.Interfaces;

// JWT ve refresh token üretimi; somut implementasyon Infrastructure/Persistence tarafındadır
public interface ITokenService
{
    AccessTokenResult CreateAccessToken(Guid userId, string email, DateTimeOffset issuedAt);

    RefreshTokenMaterial CreateRefreshToken(DateTimeOffset issuedAt);

    // Gelen düz refresh token'ın SHA-256 özetini döner
    string HashRefreshToken(string plainToken);
}
