using Praxis.Domain.Common;

namespace Praxis.Domain.Identity;

public sealed class Role : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public ICollection<User> Users { get; private set; } = new List<User>();
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private Role()
    {
    }

    public Role(string name, string description)
    {
        Name = name.Trim();
        Description = description.Trim();
    }
}

public sealed class Permission : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private Permission()
    {
    }

    public Permission(string code, string description)
    {
        Code = code.Trim();
        Description = description.Trim();
    }
}

public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    public Role Role { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;

    private RolePermission()
    {
    }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}

public sealed class User : BaseEntity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAtUtc { get; private set; }
    public Guid RoleId { get; private set; }

    public Role Role { get; private set; } = null!;
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private User()
    {
    }

    public User(string fullName, string email, string passwordHash, Guid roleId)
    {
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        RoleId = roleId;
    }

    public void UpdateProfile(string fullName, string email, Guid roleId, DateTime utcNow)
    {
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        RoleId = roleId;
        Touch(utcNow);
    }

    public void SetPassword(string passwordHash, DateTime utcNow)
    {
        PasswordHash = passwordHash;
        Touch(utcNow);
    }

    public void RegisterLogin(DateTime utcNow)
    {
        LastLoginAtUtc = utcNow;
        Touch(utcNow);
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        Touch(utcNow);
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        Touch(utcNow);
    }
}

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public User User { get; private set; } = null!;

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    private RefreshToken()
    {
    }

    public RefreshToken(Guid userId, string token, DateTime expiresAtUtc, string? createdByIp)
    {
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
    }

    public void Revoke(DateTime utcNow, string? revokedByIp, string? replacedByToken)
    {
        RevokedAtUtc = utcNow;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
        Touch(utcNow);
    }
}
