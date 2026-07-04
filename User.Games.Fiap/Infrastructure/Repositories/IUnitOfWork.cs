namespace User.Games.Fiap.Infrastructure.Repositories;

public interface IUnitOfWork
{
    IUserRepository Users { get; }

    IRefreshTokenRepository RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
