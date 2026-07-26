#nullable disable

using Xunit;
using KromicStore.Domain.Entities;

namespace KromicStore.Tests.Unit.Domain;

public class CategoryTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();

    // ─── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void CreateCategory_WithValidName_ShouldSucceed()
    {
        // Act
        var category = Category.Create(ValidTenantId, "Electronics");

        // Assert
        Assert.Equal(ValidTenantId, category.TenantId);
        Assert.Equal("Electronics", category.Name);
        Assert.Null(category.ParentCategoryId);
        Assert.Equal(0, category.NestingLevel);
    }

    [Fact]
    public void CreateCategory_WithEmptyName_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Category.Create(ValidTenantId, ""));
    }

    [Fact]
    public void CreateCategory_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Category.Create(Guid.Empty, "Electronics"));
    }

    // ─── Parent-child relationship ────────────────────────────────────────────

    [Fact]
    public void CreateCategory_WithParentId_ShouldSetParentCategoryId()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var child = Category.Create(ValidTenantId, "Smartphones", parentCategoryId: parentId);

        // Assert
        Assert.Equal(parentId, child.ParentCategoryId);
        Assert.Equal(1, child.NestingLevel);
    }

    // ─── SetParentCategory – hierarchy depth guard ────────────────────────────

    [Fact]
    public void SetParentCategory_WhenParentIsAtLevel2_ShouldThrowInvalidOperationException()
    {
        // Arrange – build a 3-level chain: root (0) → mid (1) → leaf (2)
        var root = Category.Create(ValidTenantId, "Root");
        var mid = Category.Create(ValidTenantId, "Mid", parentCategoryId: root.Id);
        // Manually promote mid to level 1 by using SetParentCategory
        mid.SetParentCategory(root.Id, new[] { root });

        var leaf = Category.Create(ValidTenantId, "Leaf");
        leaf.SetParentCategory(mid.Id, new[] { root, mid });

        // Now try to attach a 4th level (would make leaf nesting 2, then child nesting 3)
        var tooDeep = Category.Create(ValidTenantId, "TooDeep");

        // Act & Assert – leaf is at nesting level 2, so adding a child exceeds the limit
        Assert.Throws<InvalidOperationException>(() =>
            tooDeep.SetParentCategory(leaf.Id, new[] { root, mid, leaf }));
    }

    // ─── SetParentCategory – circular reference guard ─────────────────────────

    [Fact]
    public void SetParentCategory_WhenParentIsDescendant_ShouldThrowInvalidOperationException()
    {
        // Arrange – root → child
        var root = Category.Create(ValidTenantId, "Root");
        var child = Category.Create(ValidTenantId, "Child");
        child.SetParentCategory(root.Id, new[] { root });

        // Act & Assert – try to make root a child of child (circular)
        Assert.Throws<InvalidOperationException>(() =>
            root.SetParentCategory(child.Id, new[] { root, child }));
    }

    [Fact]
    public void SetParentCategory_ToSelf_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var category = Category.Create(ValidTenantId, "Self-ref");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            category.SetParentCategory(category.Id, new[] { category }));
    }

    // ─── Subcategory management ───────────────────────────────────────────────

    [Fact]
    public void AddSubcategory_ShouldRegisterChildId()
    {
        // Arrange
        var parent = Category.Create(ValidTenantId, "Parent");
        var childId = Guid.NewGuid();

        // Act
        parent.AddSubcategory(childId);

        // Assert
        Assert.Contains(childId, parent.SubcategoryIds);
    }

    [Fact]
    public void RemoveSubcategory_ShouldDeregisterChildId()
    {
        // Arrange
        var parent = Category.Create(ValidTenantId, "Parent");
        var childId = Guid.NewGuid();
        parent.AddSubcategory(childId);

        // Act
        parent.RemoveSubcategory(childId);

        // Assert
        Assert.DoesNotContain(childId, parent.SubcategoryIds);
    }
}
