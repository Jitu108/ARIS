using aris.IdentityService.Domain.Entities;

namespace aris.IdentityService.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    /// <summary>
    /// Revokes <paramref name="currentToken"/> and adds <paramref name="newToken"/> in one save.
    /// Returns false instead of throwing if <paramref name="currentToken"/> was concurrently
    /// rotated/revoked by another request since it was read — the caller must treat that the
    /// same as any other invalid-refresh-token failure, not let both callers win.
    /// </summary>
    Task<bool> RotateAsync(RefreshToken currentToken, RefreshToken newToken, CancellationToken cancellationToken);
    Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken);
}
