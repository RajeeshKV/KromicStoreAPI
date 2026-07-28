namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents an image associated with a product.
/// </summary>
public class ProductImage : BaseEntity
{
    /// <summary>Gets the product ID this image belongs to.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Gets the Cloudinary URL of the image.</summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>Gets the Cloudinary public ID for deletion/management.</summary>
    public string CloudinaryPublicId { get; private set; } = string.Empty;

    /// <summary>Gets the display order for sorting images.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Gets whether this is the primary/featured image.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Gets the alt text for accessibility.</summary>
    public string? AltText { get; private set; }

    /// <summary>Navigation property to the parent product.</summary>
    public Product? Product { get; private set; }

    /// <summary>
    /// Creates a new instance of ProductImage.
    /// </summary>
    public static ProductImage Create(
        Guid productId,
        string url,
        string cloudinaryPublicId,
        int displayOrder = 0,
        bool isPrimary = false,
        string? altText = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Image URL is required.", nameof(url));
        if (string.IsNullOrWhiteSpace(cloudinaryPublicId))
            throw new ArgumentException("Cloudinary public ID is required.", nameof(cloudinaryPublicId));

        return new ProductImage
        {
            ProductId = productId,
            Url = url,
            CloudinaryPublicId = cloudinaryPublicId,
            DisplayOrder = displayOrder,
            IsPrimary = isPrimary,
            AltText = altText
        };
    }

    /// <summary>
    /// Updates the display order.
    /// </summary>
    public void UpdateDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Sets this image as the primary image.
    /// </summary>
    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    /// <summary>
    /// Removes primary status from this image.
    /// </summary>
    public void RemovePrimaryStatus()
    {
        IsPrimary = false;
    }

    /// <summary>
    /// Updates the alt text.
    /// </summary>
    public void UpdateAltText(string? altText)
    {
        AltText = altText;
    }
}
