using User.Games.Fiap.Application.Auth;
using User.Games.Fiap.Application.Validators;

namespace User.Games.Fiap.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_IsValid()
    {
        Assert.True(_validator.Validate(new LoginRequest("joao@example.com", "qualquer")).IsValid);
    }

    [Theory]
    [InlineData("", "qualquer")]
    [InlineData("joao@example.com", "")]
    public void MissingField_IsInvalid(string email, string password)
    {
        Assert.False(_validator.Validate(new LoginRequest(email, password)).IsValid);
    }
}
