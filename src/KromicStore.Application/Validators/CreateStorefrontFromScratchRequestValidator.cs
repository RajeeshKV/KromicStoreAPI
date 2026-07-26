namespace KromicStore.Application.Validators;

using Contracts.V1.Storefront;
using FluentValidation;

/// <summary>
/// Validator for CreateStorefrontFromScratchRequest.
/// </summary>
public class CreateStorefrontFromScratchRequestValidator : AbstractValidator<CreateStorefrontFromScratchRequest>
{
    /// <summary>
    /// Initializes a new instance of the CreateStorefrontFromScratchRequestValidator class.
    /// </summary>
    public CreateStorefrontFromScratchRequestValidator()
    {
        RuleFor(x => x.StoreName)
            .NotEmpty()
            .WithMessage("Store name is required")
            .MaximumLength(100)
            .WithMessage("Store name must not exceed 100 characters");
    }
}
