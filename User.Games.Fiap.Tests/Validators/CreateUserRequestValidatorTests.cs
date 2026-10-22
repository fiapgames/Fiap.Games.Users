using User.Games.Fiap.Application.Users;
using User.Games.Fiap.Application.Validators;

namespace User.Games.Fiap.Tests.Validators;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_IsValid()
    {
        var request = new CreateUserRequest("Joao", "joao@example.com", "SenhaForte@123");

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("Curta1A")]
    [InlineData("semmaiuscula1")]
    [InlineData("SEMMINUSCULA1")]
    [InlineData("SemNumeroAqui")]
    public void WeakPassword_IsInvalid(string password)
    {
        var request = new CreateUserRequest("Joao", "joao@example.com", password);

        Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == nameof(CreateUserRequest.Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("nao-e-email")]
    public void InvalidEmail_IsInvalid(string email)
    {
        var request = new CreateUserRequest("Joao", email, "SenhaForte@123");

        Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == nameof(CreateUserRequest.Email));
    }

    [Fact]
    public void EmptyName_IsInvalid()
    {
        var request = new CreateUserRequest("", "joao@example.com", "SenhaForte@123");

        Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == nameof(CreateUserRequest.Nome));
    }
}
