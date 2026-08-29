using aris.IdentityService.Application.Abstractions;
using aris.IdentityService.Domain.Entities;
using aris.IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace aris.IdentityService.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public RefreshTokenRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return await _dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        refreshToken.RowVersion = Guid.NewGuid().ToByteArray();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RotateAsync(RefreshToken currentToken, RefreshToken newToken, CancellationToken cancellationToken)
    {
        currentToken.RevokedAtUtc = DateTime.UtcNow;
        currentToken.ReplacedByTokenId = newToken.Id;
        currentToken.RowVersion = Guid.NewGuid().ToByteArray();
        _dbContext.RefreshTokens.Add(newToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // currentToken's RowVersion changed since it was read — another concurrent refresh
            // call already rotated (or otherwise revoked) it first. Losing this race is exactly
            // the single-use guarantee working as intended, not an error to surface as one.
            return false;
        }
    }

    public async Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        var revokedAtUtc = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = revokedAtUtc;
            token.RowVersion = Guid.NewGuid().ToByteArray();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
