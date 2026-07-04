using Microsoft.EntityFrameworkCore;
using User.Games.Fiap.Infrastructure.Data;
using UserEntity = User.Games.Fiap.Domain.Entities.User;

namespace User.Games.Fiap.Infrastructure.Repositories;

public sealed class UserRepository(ApplicationDbContext dbContext)
    : GenericRepository<UserEntity>(dbContext), IUserRepository
{
    public Task<UserEntity?> GetByEmailAsync(string normalizedEmail, bool asNoTracking, CancellationToken cancellationToken)
    {
        return Query(asNoTracking)
            .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return Query()
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }
}
