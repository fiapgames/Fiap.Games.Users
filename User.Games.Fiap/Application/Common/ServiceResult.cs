namespace User.Games.Fiap.Application.Common;

public sealed record ServiceResult<T>(bool Succeeded, int StatusCode, T? Value = default, string? Error = null)
{
    public static ServiceResult<T> Ok(T value)
    {
        return new ServiceResult<T>(true, StatusCodes.Status200OK, value);
    }

    public static ServiceResult<T> Created(T value)
    {
        return new ServiceResult<T>(true, StatusCodes.Status201Created, value);
    }

    public static ServiceResult<T> Unauthorized(string error)
    {
        return new ServiceResult<T>(false, StatusCodes.Status401Unauthorized, default, error);
    }

    public static ServiceResult<T> NotFound(string error)
    {
        return new ServiceResult<T>(false, StatusCodes.Status404NotFound, default, error);
    }

    public static ServiceResult<T> Conflict(string error)
    {
        return new ServiceResult<T>(false, StatusCodes.Status409Conflict, default, error);
    }

    public static ServiceResult<T> BadRequest(string error)
    {
        return new ServiceResult<T>(false, StatusCodes.Status400BadRequest, default, error);
    }
}
