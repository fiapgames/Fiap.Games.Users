using UserEntity = User.Games.Fiap.Domain.Entities.User;

namespace User.Games.Fiap.Infrastructure.Repositories;

public interface IUserRepository : IRepository<UserEntity>
{
    Task<UserEntity?> GetByEmailAsync(string normalizedEmail, bool asNoTracking, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
}
