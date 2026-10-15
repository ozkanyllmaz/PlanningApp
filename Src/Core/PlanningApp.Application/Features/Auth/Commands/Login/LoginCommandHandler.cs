using System;
using MediatR;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Auth;

namespace PlanningApp.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var check = await identityService.CheckCredentialsAsync(request.Email, request.Password, cancellationToken);

        // Bilinmeyen e-posta, yanlış parola ve kilitli hesap aynı genel mesajı alır; token üretilmez, kayıt yapılmaz
        if (!check.Succeeded || check.UserId is not { } userId || check.Email is null)
            throw new AuthenticationFailedException(AuthenticationFailedException.InvalidCredentialsMessage);

        var now = timeProvider.GetUtcNow();

        var refreshToken = tokenService.CreateRefreshToken(now);
        var tokenEntity = RefreshToken.Create(
            Guid.NewGuid(), userId, refreshToken.TokenHash, now, refreshToken.ExpiresAt);

        await refreshTokenRepository.AddAsync(tokenEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.CreateAccessToken(userId, check.Email, now);

        return new AuthResult(
            userId,
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.PlainToken,
            refreshToken.ExpiresAt);
    }
}
