using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using User.Games.Fiap.Domain.Entities;

namespace User.Games.Fiap.Infrastructure.Data.Configurations;

public static class BaseEntityConfiguration
{
    public static void ConfigureBaseEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : BaseEntity
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.CreatedBy)
            .HasMaxLength(128);

        builder.Property(entity => entity.UpdatedBy)
            .HasMaxLength(128);

        builder.Property(entity => entity.DeletedBy)
            .HasMaxLength(128);

        builder.Property(entity => entity.RowVersion)
            .IsRowVersion();

        builder.HasIndex(entity => entity.IsDeleted);
    }
}
