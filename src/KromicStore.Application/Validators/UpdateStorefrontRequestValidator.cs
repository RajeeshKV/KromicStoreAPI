namespace KromicStore.Application.Validators;

using Contracts.V1.Storefront;
using FluentValidation;

/// <summary>
/// Validator for UpdateStorefrontRequest.
/// </summary>
public class UpdateStorefrontRequestValidator : AbstractValidator<UpdateStorefrontRequest>
{
    /// <summary>
    /// Initializes a new instance of the UpdateStorefrontRequestValidator class.
    /// </summary>
    public UpdateStorefrontRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .WithMessage("Store name must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Currency)
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters (ISO 4217)")
            .When(x => !string.IsNullOrEmpty(x.Currency));

        RuleFor(x => x.Country)
            .Length(2)
            .WithMessage("Country code must be exactly 2 characters (ISO 3166-1)")
            .When(x => !string.IsNullOrEmpty(x.Country));
    }
}
