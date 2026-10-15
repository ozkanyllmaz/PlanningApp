using Microsoft.EntityFrameworkCore;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Auth;

namespace PlanningApp.Persistence.Repositories;

// Repository'ler SaveChanges ÇAĞIRMAZ; kaydetme handler'daki IUnitOfWork çağrısıyla yapılır
public sealed class RefreshTokenRepository(PlanningDbContext context) : IRefreshTokenRepository
{
    // Takipli (tracked) döner: handler iptal edip kaydeder
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
        => context.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
        => await context.RefreshTokens.AddAsync(token, cancellationToken);
}
