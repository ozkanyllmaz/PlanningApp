using System;
using MediatR;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Auth;

namespace PlanningApp.Application.Features.Auth.Commands.RefreshAccessToken;

public sealed class RefreshAccessTokenCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RefreshAccessTokenCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var existing = await refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        // Bilinmeyen, iptal edilmiş veya süresi dolmuş token: istemci yeniden giriş ister
        if (existing is null || !existing.IsActive(now))
            throw new AuthenticationFailedException(AuthenticationFailedException.InvalidSessionMessage);

        var user = await identityService.FindByIdAsync(existing.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException(AuthenticationFailedException.InvalidSessionMessage);

        // Rotation: eski token iptal edilir (ReplacedByTokenId = yeni Id), yenisi eklenir
        var newMaterial = tokenService.CreateRefreshToken(now);
        var newToken = RefreshToken.Create(
            Guid.NewGuid(), user.UserId, newMaterial.TokenHash, now, newMaterial.ExpiresAt);

        existing.Revoke(now, newToken.Id);
        await refreshTokenRepository.AddAsync(newToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.CreateAccessToken(user.UserId, user.Email, now);

        return new AuthResult(
            user.UserId,
            accessToken.Token,
            accessToken.ExpiresAt,
            newMaterial.PlainToken,
            newMaterial.ExpiresAt);
    }
}
