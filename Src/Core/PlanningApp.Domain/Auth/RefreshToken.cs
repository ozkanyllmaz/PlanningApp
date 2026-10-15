using System;
using PlanningApp.Domain.Common;

namespace PlanningApp.Domain.Auth;

// Düz token SAKLANMAZ; yalnızca SHA-256 özeti (TokenHash) tutulur
public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        RevokedAt = null;
        ReplacedByTokenId = null;
    }

    public static RefreshToken Create(
        Guid id, Guid userId, string tokenHash, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Kullanıcı kimliği boş olamaz.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token özeti boş olamaz.");

        if (expiresAt <= createdAt)
            throw new DomainException("Token bitiş zamanı oluşturulma zamanından sonra olmalıdır.");

        return new RefreshToken(id, userId, tokenHash, createdAt, expiresAt);
    }

    // İptal edilmemiş ve süresi dolmamışsa aktiftir (tam ExpiresAt anında artık aktif değildir)
    public bool IsActive(DateTimeOffset asOf)
        => RevokedAt is null && asOf < ExpiresAt;

    // Yenilemede (rotation) yerine geçen token'ın Id'si verilir; çıkışta null kalır
    public void Revoke(DateTimeOffset revokedAt, Guid? replacedByTokenId = null)
    {
        if (RevokedAt is not null)
            throw new DomainException("Token zaten iptal edilmiş.");

        RevokedAt = revokedAt;
        ReplacedByTokenId = replacedByTokenId;
    }
}
