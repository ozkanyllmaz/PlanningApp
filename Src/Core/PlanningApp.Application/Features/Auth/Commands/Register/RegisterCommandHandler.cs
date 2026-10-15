using System;
using System.Linq;
using FluentValidation.Results;
using MediatR;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Auth;
using PlanningApp.Domain.Users;

namespace PlanningApp.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IUserProfileRepository userProfileRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RegisterCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // UserId sunucuda üretilir (offline kayıt yok)
        var userId = Guid.NewGuid();

        var identityResult = await identityService.CreateUserAsync(
            userId, request.Email, request.Password, cancellationToken);

        // Kimlik hatası (e-posta çakışması, parola kuralı): alan hatalarıyla ValidationException, hiçbir şey kaydedilmez
        if (!identityResult.Succeeded)
        {
            var failures = identityResult.Errors.SelectMany(
                error => error.Value.Select(message => new ValidationFailure(error.Key, message)));

            throw new ValidationException(failures);
        }

        var refreshToken = tokenService.CreateRefreshToken(now);

        // Identity ve uygulama verisi ayrı işlemlerdir; kullanıcı oluştuktan sonra bir şey ters giderse
        // yarım kalan kullanıcı telafi olarak silinir ve hata yeniden fırlatılır.
        try
        {
            var profile = UserProfile.Create(userId, request.TimeZoneId, now);
            var tokenEntity = RefreshToken.Create(
                Guid.NewGuid(), userId, refreshToken.TokenHash, now, refreshToken.ExpiresAt);

            await userProfileRepository.AddAsync(profile, cancellationToken);
            await refreshTokenRepository.AddAsync(tokenEntity, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // İstek iptal edilmiş olsa bile telafi çalışmalıdır. Telafinin kendisi hata verirse asıl
            // hata MASKELENMEZ: telafi hatası yutulur ve ilk hata yeniden fırlatılır.
            // TODO: Application'a günlükleme eklenince telafi hatası loglanacak (yetim kullanıcı onarımı).
            try
            {
                await identityService.DeleteUserAsync(userId, CancellationToken.None);
            }
            catch
            {
                // yutulur
            }

            throw;
        }

        var accessToken = tokenService.CreateAccessToken(userId, request.Email, now);

        return new AuthResult(
            userId,
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.PlainToken,
            refreshToken.ExpiresAt);
    }
}
