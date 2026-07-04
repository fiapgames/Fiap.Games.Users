using User.Games.Fiap.Infrastructure.Data;

namespace User.Games.Fiap.Infrastructure.Repositories;

public sealed class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    private IUserRepository? users;
    private IRefreshTokenRepository? refreshTokens;

    public IUserRepository Users => users ??= new UserRepository(dbContext);

    public IRefreshTokenRepository RefreshTokens => refreshTokens ??= new RefreshTokenRepository(dbContext);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
