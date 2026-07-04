using User.Games.Fiap.Application.Common;

namespace User.Games.Fiap.Application.Users;

public interface IUserService
{
    Task<ServiceResult<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<PagedResponse<UserResponse>>> GetAllAsync(int page, int pageSize, string? search, CancellationToken cancellationToken);

    Task<ServiceResult<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
