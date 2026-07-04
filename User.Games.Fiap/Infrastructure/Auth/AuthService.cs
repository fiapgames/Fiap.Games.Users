using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using User.Games.Fiap.Application.Auth;
using User.Games.Fiap.Application.Common;
using User.Games.Fiap.Application.Users;
using User.Games.Fiap.Configuration;
using User.Games.Fiap.Domain.Entities;
using User.Games.Fiap.Infrastructure.Repositories;
using UserEntity = User.Games.Fiap.Domain.Entities.User;

namespace User.Games.Fiap.Infrastructure.Auth;

public sealed class AuthService(
    IUnitOfWork unitOfWork,
    IPasswordHasher<UserEntity> passwordHasher,
    IOptions<JwtOptions> jwtOptions,
    IMapper mapper) : IAuthService
{
    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(NormalizeEmail(request.Email), false, cancellationToken);
        if (user is null)
        {
            return ServiceResult<AuthResponse>.Unauthorized("Invalid email or password.");
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return ServiceResult<AuthResponse>.Unauthorized("Invalid email or password.");
        }

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.Password = passwordHasher.HashPassword(user, request.Password);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;

        var refreshToken = CreateRefreshToken(user.Id, ipAddress);
        await unitOfWork.RefreshTokens.AddAsync(refreshToken.Entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Ok(CreateAuthResponse(user, refreshToken.RawToken));
    }

    public async Task<ServiceResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive || storedToken.User.IsDeleted)
        {
            return ServiceResult<AuthResponse>.Unauthorized("Invalid refresh token.");
        }

        var replacement = CreateRefreshToken(storedToken.UserId, ipAddress);
        storedToken.Revoke(replacement.Entity.TokenHash, ipAddress);

        await unitOfWork.RefreshTokens.AddAsync(replacement.Entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Ok(CreateAuthResponse(storedToken.User, replacement.RawToken));
    }

    private AuthResponse CreateAuthResponse(UserEntity user, string refreshToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenMinutes);
        var accessToken = GenerateAccessToken(user, expiresAt);

        return new AuthResponse(
            accessToken,
            refreshToken,
            "Bearer",
            expiresAt,
            mapper.Map<UserResponse>(user));
    }

    private string GenerateAccessToken(UserEntity user, DateTimeOffset expiresAt)
    {
        var options = jwtOptions.Value;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Nome),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshTokenPair CreateRefreshToken(Guid userId, string? ipAddress)
    {
        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        return new RefreshTokenPair(rawToken, entity);
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private sealed record RefreshTokenPair(string RawToken, RefreshToken Entity);
}
