namespace KromicStore.Domain.Enums;

/// <summary>
/// Enumeration of component types available for storefront pages.
/// </summary>
public enum ComponentType
{
    /// <summary>Hero section with banner image and call-to-action.</summary>
    Hero = 1,

    /// <summary>Banner with promotional message.</summary>
    Banner = 2,

    /// <summary>Grid of products for browsing.</summary>
    ProductGrid = 3,

    /// <summary>Grid of categories for navigation.</summary>
    CategoryGrid = 4,

    /// <summary>Newsletter subscription form.</summary>
    Newsletter = 5,

    /// <summary>Frequently asked questions section.</summary>
    FAQ = 6,

    /// <summary>Testimonials from customers.</summary>
    Testimonials = 7,

    /// <summary>Text block with custom content.</summary>
    TextBlock = 8,

    /// <summary>Image block with caption.</summary>
    ImageBlock = 9,

    /// <summary>Video block with embedded video.</summary>
    VideoBlock = 10,

    /// <summary>Call-to-action button component.</summary>
    ButtonBlock = 11,

    /// <summary>Social media links component.</summary>
    SocialLinks = 12,

    /// <summary>Contact form component.</summary>
    ContactForm = 13,

    /// <summary>Featured products carousel.</summary>
    FeaturedProductsCarousel = 14,

    /// <summary>Image carousel/slider.</summary>
    ImageCarousel = 15,

    /// <summary>Testimonials carousel.</summary>
    TestimonialsCarousel = 16,

    /// <summary>Custom HTML block.</summary>
    CustomHTML = 17
}
