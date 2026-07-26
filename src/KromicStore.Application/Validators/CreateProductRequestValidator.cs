namespace KromicStore.Application.Validators;

using FluentValidation;
using KromicStore.Contracts.V1.Products;

/// <summary>
/// Validator for product creation requests.
/// </summary>
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    /// <summary>
    /// Creates a new instance of CreateProductRequestValidator.
    /// </summary>
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        RuleFor(x => x.CostPrice)
            .GreaterThan(0).WithMessage("Cost price must be greater than zero.")
            .When(x => x.CostPrice.HasValue);
    }
}
