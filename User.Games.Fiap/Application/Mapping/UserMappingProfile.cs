using AutoMapper;
using User.Games.Fiap.Application.Users;
using UserEntity = User.Games.Fiap.Domain.Entities.User;

namespace User.Games.Fiap.Application.Mapping;

public sealed class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<UserEntity, UserResponse>();
    }
}
