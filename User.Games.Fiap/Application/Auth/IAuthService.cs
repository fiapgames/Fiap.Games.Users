using User.Games.Fiap.Application.Common;

namespace User.Games.Fiap.Application.Auth;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<ServiceResult<AuthResponse>> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken);
}
