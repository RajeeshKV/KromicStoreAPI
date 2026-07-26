namespace KromicStore.Application.Validators;

using FluentValidation;
using KromicStore.Contracts.V1.Auth;

/// <summary>
/// Validator for login requests.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>
    /// Creates a new instance of LoginRequestValidator.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
    }
}
