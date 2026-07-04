using Microsoft.EntityFrameworkCore;
using User.Games.Fiap.Domain.Entities;
using User.Games.Fiap.Infrastructure.Data;

namespace User.Games.Fiap.Infrastructure.Repositories;

public class GenericRepository<TEntity>(ApplicationDbContext dbContext) : IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected ApplicationDbContext DbContext { get; } = dbContext;

    protected DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

    public IQueryable<TEntity> Query(bool asNoTracking = true)
    {
        return asNoTracking ? DbSet.AsNoTracking() : DbSet;
    }

    public Task<TEntity?> GetByIdAsync(Guid id, bool asNoTracking, CancellationToken cancellationToken)
    {
        return Query(asNoTracking).FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }
}
