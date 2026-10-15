using PlanningApp.Domain.Auth;

namespace PlanningApp.Application.Common.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    // Özet ile arar; takipli (tracked) döner, yoksa null
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);
}
