using System;
using MediatR;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;

namespace PlanningApp.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var token = await refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        // Idempotent: token yoksa veya zaten iptalse hiçbir şey yapmadan dön (bilgi sızdırılmaz)
        if (token is null || token.RevokedAt is not null)
            return;

        token.Revoke(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
