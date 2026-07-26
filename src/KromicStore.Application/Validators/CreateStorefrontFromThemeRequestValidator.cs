namespace KromicStore.Application.Validators;

using Contracts.V1.Storefront;
using FluentValidation;

/// <summary>
/// Validator for CreateStorefrontFromThemeRequest.
/// </summary>
public class CreateStorefrontFromThemeRequestValidator : AbstractValidator<CreateStorefrontFromThemeRequest>
{
    /// <summary>
    /// Initializes a new instance of the CreateStorefrontFromThemeRequestValidator class.
    /// </summary>
    public CreateStorefrontFromThemeRequestValidator()
    {
        RuleFor(x => x.ThemeId)
            .NotEmpty()
            .WithMessage("Theme ID is required");

        RuleFor(x => x.StoreName)
            .NotEmpty()
            .WithMessage("Store name is required")
            .MaximumLength(200)
            .WithMessage("Store name must not exceed 200 characters");
    }
}
