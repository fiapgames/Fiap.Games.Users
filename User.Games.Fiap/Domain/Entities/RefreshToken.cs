using System.ComponentModel.DataAnnotations.Schema;

namespace User.Games.Fiap.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? CreatedByIp { get; set; }

    public string? RevokedByIp { get; set; }

    [NotMapped]
    public bool IsActive => !IsDeleted && RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;

    public void Revoke(string? replacedByTokenHash, string? ipAddress)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
        RevokedByIp = ipAddress;
    }
}
