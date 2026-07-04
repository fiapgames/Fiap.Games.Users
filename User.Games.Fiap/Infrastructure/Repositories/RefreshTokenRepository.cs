using Microsoft.EntityFrameworkCore;
using User.Games.Fiap.Domain.Entities;
using User.Games.Fiap.Infrastructure.Data;

namespace User.Games.Fiap.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(ApplicationDbContext dbContext)
    : GenericRepository<RefreshToken>(dbContext), IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return Query(asNoTracking: false)
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }
}
