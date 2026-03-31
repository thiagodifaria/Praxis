using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Common;
using Praxis.Application.Models;
using Praxis.Application.Persistence;

namespace Praxis.Application.Services;

public sealed class AuthService(
    IPraxisDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IClock clock)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .Include(current => current.Role)
            .ThenInclude(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .Include(current => current.RefreshTokens)
            .FirstOrDefaultAsync(current => current.Email == normalizedEmail, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!user.IsActive)
        {
            throw new ForbiddenException("Inactive users cannot authenticate.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ForbiddenException("Invalid credentials.");
        }

        var now = clock.UtcNow;
        var permissions = user.Role.RolePermissions.Select(rolePermission => rolePermission.Permission.Code).ToArray();

        user.RegisterLogin(now);

        var refreshToken = new Domain.Identity.RefreshToken(
            user.Id,
            tokenService.GenerateRefreshToken(),
            now.AddDays(14),
            ipAddress);

        refreshToken.SetCreatedAt(now);

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            tokenService.CreateAccessToken(user.Id, user.FullName, user.Email, user.Role.Name, permissions),
            refreshToken.Token,
            refreshToken.ExpiresAtUtc,
            new AuthUserResponse(user.Id, user.FullName, user.Email, user.Role.Name, permissions));
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user.Role)
            .ThenInclude(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(token => token.Token == request.RefreshToken, cancellationToken)
            ?? throw new ForbiddenException("Refresh token is invalid.");

        if (!storedToken.IsActive)
        {
            throw new ForbiddenException("Refresh token has expired or was revoked.");
        }

        var now = clock.UtcNow;
        var replacementToken = tokenService.GenerateRefreshToken();
        storedToken.Revoke(now, ipAddress, replacementToken);

        var newRefreshToken = new Domain.Identity.RefreshToken(storedToken.UserId, replacementToken, now.AddDays(14), ipAddress);
        newRefreshToken.SetCreatedAt(now);
        dbContext.RefreshTokens.Add(newRefreshToken);

        var permissions = storedToken.User.Role.RolePermissions.Select(rolePermission => rolePermission.Permission.Code).ToArray();
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            tokenService.CreateAccessToken(
                storedToken.User.Id,
                storedToken.User.FullName,
                storedToken.User.Email,
                storedToken.User.Role.Name,
                permissions),
            newRefreshToken.Token,
            newRefreshToken.ExpiresAtUtc,
            new AuthUserResponse(
                storedToken.User.Id,
                storedToken.User.FullName,
                storedToken.User.Email,
                storedToken.User.Role.Name,
                permissions));
    }
}
