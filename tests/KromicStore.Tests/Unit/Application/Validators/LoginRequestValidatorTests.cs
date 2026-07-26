namespace KromicStore.Tests.Unit.Application.Validators;

using KromicStore.Application.Validators;
using KromicStore.Contracts.V1.Auth;
using Xunit;

/// <summary>
/// Unit tests for the LoginRequestValidator.
/// </summary>
public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ShouldSucceed()
    {
        // Arrange
        var request = new LoginRequest("user@example.com", "Password123");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithEmptyEmail_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest("", "Password123");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest("invalid-email", "Password123");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithShortPassword_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest("user@example.com", "123");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
    }
}
