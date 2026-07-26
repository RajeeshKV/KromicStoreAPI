#nullable disable

using Xunit;
using KromicStore.Domain.ValueObjects;

namespace KromicStore.Tests.Unit.Domain;

/// <summary>
/// Unit tests for MandatoryFields value object validating field tracking and status management.
/// Tests: creation, placeholder tracking, field counting, and status updates.
/// </summary>
public class MandatoryFieldsTests
{
    #region Creation Tests

    [Fact]
    public void CreateAllPlaceholders_ReturnsAllFieldsAsPlaceholders()
    {
        // Act
        var fields = MandatoryFields.CreateAllPlaceholders();

        // Assert
        Assert.True(fields.IsStoreNamePlaceholder);
        Assert.True(fields.IsLogoPlaceholder);
        Assert.True(fields.IsEmailPlaceholder);
        Assert.True(fields.IsPhonePlaceholder);
        Assert.True(fields.IsAddressPlaceholder);
        Assert.True(fields.IsCurrencyPlaceholder);
        Assert.True(fields.IsCountryPlaceholder);
        Assert.True(fields.IsBrandColorPlaceholder);
        Assert.True(fields.IsCopyrightPlaceholder);
    }

    #endregion

    #region Field Count Tests

    [Fact]
    public void GetProvidedFieldCount_WithAllPlaceholders_ReturnsZero()
    {
        // Arrange
        var fields = MandatoryFields.CreateAllPlaceholders();

        // Act
        var count = fields.GetProvidedFieldCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetProvidedFieldCount_WithAllFieldsProvided_ReturnsNine()
    {
        // Arrange
        var fields = new MandatoryFields
        {
            IsStoreNamePlaceholder = false,
            IsLogoPlaceholder = false,
            IsEmailPlaceholder = false,
            IsPhonePlaceholder = false,
            IsAddressPlaceholder = false,
            IsCurrencyPlaceholder = false,
            IsCountryPlaceholder = false,
            IsBrandColorPlaceholder = false,
            IsCopyrightPlaceholder = false
        };

        // Act
        var count = fields.GetProvidedFieldCount();

        // Assert
        Assert.Equal(9, count);
    }

    [Fact]
    public void GetProvidedFieldCount_WithPartialFields_ReturnsCorrectCount()
    {
        // Arrange
        var fields = new MandatoryFields
        {
            IsStoreNamePlaceholder = false,
            IsLogoPlaceholder = false,
            IsEmailPlaceholder = false,
            IsPhonePlaceholder = true, // Placeholder
            IsAddressPlaceholder = true, // Placeholder
            IsCurrencyPlaceholder = false,
            IsCountryPlaceholder = false,
            IsBrandColorPlaceholder = true, // Placeholder
            IsCopyrightPlaceholder = false
        };

        // Act
        var count = fields.GetProvidedFieldCount();

        // Assert
        Assert.Equal(6, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(9)]
    public void GetProvidedFieldCount_WithVariousCounts_ReturnsAccurateCount(int providedFieldCount)
    {
        // Arrange - Create fields with specified count of provided fields
        var fields = MandatoryFields.CreateAllPlaceholders();
        var fieldNames = new[]
        {
            "storename", "logo", "email", "phone", "address",
            "currency", "country", "brandcolor", "copyright"
        };

        for (int i = 0; i < providedFieldCount; i++)
        {
            fields = fields.WithFieldUpdated(fieldNames[i], false);
        }

        // Act
        var count = fields.GetProvidedFieldCount();

        // Assert
        Assert.Equal(providedFieldCount, count);
    }

    #endregion

    #region All Fields Provided Tests

    [Fact]
    public void AreAllFieldsProvided_WithAllPlaceholders_ReturnsFalse()
    {
        // Arrange
        var fields = MandatoryFields.CreateAllPlaceholders();

        // Act
        var result = fields.AreAllFieldsProvided();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AreAllFieldsProvided_WithAllFieldsProvided_ReturnsTrue()
    {
        // Arrange
        var fields = new MandatoryFields
        {
            IsStoreNamePlaceholder = false,
            IsLogoPlaceholder = false,
            IsEmailPlaceholder = false,
            IsPhonePlaceholder = false,
            IsAddressPlaceholder = false,
            IsCurrencyPlaceholder = false,
            IsCountryPlaceholder = false,
            IsBrandColorPlaceholder = false,
            IsCopyrightPlaceholder = false
        };

        // Act
        var result = fields.AreAllFieldsProvided();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreAllFieldsProvided_WithSingleFieldMissing_ReturnsFalse()
    {
        // Arrange
        var fields = new MandatoryFields
        {
            IsStoreNamePlaceholder = false,
            IsLogoPlaceholder = false,
            IsEmailPlaceholder = false,
            IsPhonePlaceholder = false,
            IsAddressPlaceholder = false,
            IsCurrencyPlaceholder = false,
            IsCountryPlaceholder = false,
            IsBrandColorPlaceholder = false,
            IsCopyrightPlaceholder = true // Missing this field
        };

        // Act
        var result = fields.AreAllFieldsProvided();

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("storename")]
    [InlineData("logo")]
    [InlineData("email")]
    [InlineData("phone")]
    [InlineData("address")]
    [InlineData("currency")]
    [InlineData("country")]
    [InlineData("brandcolor")]
    [InlineData("copyright")]
    public void AreAllFieldsProvided_WithEachFieldMissing_ReturnsFalse(string missingField)
    {
        // Arrange
        var fields = new MandatoryFields
        {
            IsStoreNamePlaceholder = false,
            IsLogoPlaceholder = false,
            IsEmailPlaceholder = false,
            IsPhonePlaceholder = false,
            IsAddressPlaceholder = false,
            IsCurrencyPlaceholder = false,
            IsCountryPlaceholder = false,
            IsBrandColorPlaceholder = false,
            IsCopyrightPlaceholder = false
        };
        fields = fields.WithFieldUpdated(missingField, true); // Set one as placeholder

        // Act
        var result = fields.AreAllFieldsProvided();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Field Update Tests

    [Theory]
    [InlineData("storename", nameof(MandatoryFields.IsStoreNamePlaceholder))]
    [InlineData("logo", nameof(MandatoryFields.IsLogoPlaceholder))]
    [InlineData("email", nameof(MandatoryFields.IsEmailPlaceholder))]
    [InlineData("phone", nameof(MandatoryFields.IsPhonePlaceholder))]
    [InlineData("address", nameof(MandatoryFields.IsAddressPlaceholder))]
    [InlineData("currency", nameof(MandatoryFields.IsCurrencyPlaceholder))]
    [InlineData("country", nameof(MandatoryFields.IsCountryPlaceholder))]
    [InlineData("brandcolor", nameof(MandatoryFields.IsBrandColorPlaceholder))]
    [InlineData("copyright", nameof(MandatoryFields.IsCopyrightPlaceholder))]
    public void WithFieldUpdated_UpdatesSpecificField(string fieldName, string propertyName)
    {
        // Arrange
        var fields = MandatoryFields.CreateAllPlaceholders();

        // Act
        var updated = fields.WithFieldUpdated(fieldName, false);

        // Assert
        var property = typeof(MandatoryFields).GetProperty(propertyName);
        Assert.NotNull(property);
        Assert.False((bool)property.GetValue(updated));
    }

    [Fact]
    public void WithFieldUpdated_ToLowercase_Normalizes()
    {
        // Arrange
        var fields = MandatoryFields.CreateAllPlaceholders();

        // Act
        var updated1 = fields.WithFieldUpdated("StoreName", false);
        var updated2 = fields.WithFieldUpdated("STORENAME", false);
        var updated3 = fields.WithFieldUpdated("storename", false);

        // Assert
        Assert.False(updated1.IsStoreNamePlaceholder);
        Assert.False(updated2.IsStoreNamePlaceholder);
        Assert.False(updated3.IsStoreNamePlaceholder);
    }

    [Fact]
    public void WithFieldUpdated_ToPlaceholder_SetFieldAsPlaceholder()
    {
        // Arrange
        var fields = new MandatoryFields
        {
            IsStoreNamePlaceholder = false,
            IsLogoPlaceholder = false,
            IsEmailPlaceholder = false,
            IsPhonePlaceholder = false,
            IsAddressPlaceholder = false,
            IsCurrencyPlaceholder = false,
            IsCountryPlaceholder = false,
            IsBrandColorPlaceholder = false,
            IsCopyrightPlaceholder = false
        };

        // Act
        var updated = fields.WithFieldUpdated("email", true);

        // Assert
        Assert.True(updated.IsEmailPlaceholder);
        Assert.False(updated.IsStoreNamePlaceholder); // Others unchanged
    }

    [Fact]
    public void WithFieldUpdated_WithInvalidFieldName_ReturnsUnchanged()
    {
        // Arrange
        var fields = MandatoryFields.CreateAllPlaceholders();

        // Act
        var updated = fields.WithFieldUpdated("invalidfield", false);

        // Assert
        Assert.Equal(fields, updated);
    }

    [Fact]
    public void WithFieldUpdated_WithNullFieldName_ReturnsUnchanged()
    {
        // Arrange
        var fields = MandatoryFields.CreateAllPlaceholders();

        // Act
        var updated = fields.WithFieldUpdated(null, false);

        // Assert
        Assert.Equal(fields, updated);
    }

    [Fact]
    public void WithFieldUpdated_Immutable_DoesNotMutateOriginal()
    {
        // Arrange
        var fields = MandatoryFields.CreateAllPlaceholders();

        // Act
        var updated = fields.WithFieldUpdated("storename", false);

        // Assert
        Assert.True(fields.IsStoreNamePlaceholder); // Original unchanged
        Assert.False(updated.IsStoreNamePlaceholder); // New instance has change
    }

    #endregion

    #region Progressive Provision Tests

    [Fact]
    public void ProgressivelyProvideFields_UpdatesCountCorrectly()
    {
        // Arrange
        var fields = MandatoryFields.CreateAllPlaceholders();
        Assert.Equal(0, fields.GetProvidedFieldCount());

        // Act & Assert
        fields = fields.WithFieldUpdated("storename", false);
        Assert.Equal(1, fields.GetProvidedFieldCount());

        fields = fields.WithFieldUpdated("logo", false);
        Assert.Equal(2, fields.GetProvidedFieldCount());

        fields = fields.WithFieldUpdated("email", false);
        Assert.Equal(3, fields.GetProvidedFieldCount());

        // Continue until all are provided
        fields = fields.WithFieldUpdated("phone", false);
        fields = fields.WithFieldUpdated("address", false);
        fields = fields.WithFieldUpdated("currency", false);
        fields = fields.WithFieldUpdated("country", false);
        fields = fields.WithFieldUpdated("brandcolor", false);
        fields = fields.WithFieldUpdated("copyright", false);

        Assert.Equal(9, fields.GetProvidedFieldCount());
        Assert.True(fields.AreAllFieldsProvided());
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_TwoInstancesWithSameValues_AreEqual()
    {
        // Arrange
        var fields1 = MandatoryFields.CreateAllPlaceholders();
        var fields2 = MandatoryFields.CreateAllPlaceholders();

        // Act & Assert
        Assert.Equal(fields1, fields2);
    }

    [Fact]
    public void Equality_InstancesWithDifferentValues_AreNotEqual()
    {
        // Arrange
        var fields1 = MandatoryFields.CreateAllPlaceholders();
        var fields2 = fields1.WithFieldUpdated("storename", false);

        // Act & Assert
        Assert.NotEqual(fields1, fields2);
    }

    #endregion
}
