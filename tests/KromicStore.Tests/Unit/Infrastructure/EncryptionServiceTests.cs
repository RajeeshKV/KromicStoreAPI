#nullable disable

using KromicStore.Infrastructure.Services;
using Xunit;

namespace KromicStore.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for EncryptionService validating AES encryption/decryption, key generation, and edge cases.
/// </summary>
public class EncryptionServiceTests
{
    private readonly string _validEncryptionKey;
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        // Generate a valid 256-bit (32 bytes) key for AES-256
        _validEncryptionKey = GenerateValidKey();
        _encryptionService = new EncryptionService(_validEncryptionKey);
    }

    // ==================== Constructor Tests ====================

    [Fact]
    public void Constructor_WithValidKey_ShouldSucceed()
    {
        // Act & Assert
        var service = new EncryptionService(_validEncryptionKey);
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EncryptionService(null));
    }

    [Fact]
    public void Constructor_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EncryptionService(string.Empty));
    }

    [Fact]
    public void Constructor_WithWhitespaceKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EncryptionService("   "));
    }

    [Fact]
    public void Constructor_WithInvalidBase64Key_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EncryptionService("not-valid-base64!!!"));
    }

    [Fact]
    public void Constructor_WithWrongKeyLength_ShouldThrowArgumentException()
    {
        // Arrange - Create a 16-byte key (AES-128 instead of AES-256)
        var invalidKey = Convert.ToBase64String(new byte[16]);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EncryptionService(invalidKey));
    }

    // ==================== EncryptAsync Tests ====================

    [Fact]
    public async Task EncryptAsync_WithPlaintext_ShouldReturnBase64Ciphertext()
    {
        // Arrange
        const string plaintext = "This is a secret message";

        // Act
        var ciphertext = await _encryptionService.EncryptAsync(plaintext);

        // Assert
        Assert.NotNull(ciphertext);
        Assert.NotEmpty(ciphertext);

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(ciphertext);
        Assert.NotEmpty(bytes);

        // Verify ciphertext is not equal to plaintext
        Assert.NotEqual(plaintext, ciphertext);
    }

    [Fact]
    public async Task EncryptAsync_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _encryptionService.EncryptAsync(string.Empty);
        });
    }

    [Fact]
    public async Task EncryptAsync_WithNullString_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _encryptionService.EncryptAsync(null);
        });
    }

    [Fact]
    public async Task EncryptAsync_WithLongText_ShouldHandleCorrectly()
    {
        // Arrange
        var longText = new string('A', 10000);

        // Act
        var ciphertext = await _encryptionService.EncryptAsync(longText);

        // Assert
        Assert.NotNull(ciphertext);
        Assert.NotEmpty(ciphertext);

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(ciphertext);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task EncryptAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        const string plaintext = "Special chars: !@#$%^&*()_+-=[]{}|;:',.<>?/~`";

        // Act
        var ciphertext = await _encryptionService.EncryptAsync(plaintext);

        // Assert
        Assert.NotNull(ciphertext);
        Assert.NotEmpty(ciphertext);

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(ciphertext);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task EncryptAsync_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        const string plaintext = "Unicode: 你好 مرحبا 🔐";

        // Act
        var ciphertext = await _encryptionService.EncryptAsync(plaintext);

        // Assert
        Assert.NotNull(ciphertext);
        Assert.NotEmpty(ciphertext);

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(ciphertext);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task EncryptAsync_SamePlaintextTwice_ShouldProduceDifferentCiphertexts()
    {
        // Arrange
        const string plaintext = "Same plaintext";

        // Act
        var ciphertext1 = await _encryptionService.EncryptAsync(plaintext);
        var ciphertext2 = await _encryptionService.EncryptAsync(plaintext);

        // Assert
        Assert.NotEqual(ciphertext1, ciphertext2);
    }

    // ==================== DecryptAsync Tests ====================

    [Fact]
    public async Task DecryptAsync_WithEncryptedCiphertext_ShouldReturnOriginalPlaintext()
    {
        // Arrange
        const string plaintext = "This is a secret message";
        var ciphertext = await _encryptionService.EncryptAsync(plaintext);

        // Act
        var decryptedText = await _encryptionService.DecryptAsync(ciphertext);

        // Assert
        Assert.Equal(plaintext, decryptedText);
    }

    [Fact]
    public async Task DecryptAsync_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _encryptionService.DecryptAsync(string.Empty);
        });
    }

    [Fact]
    public async Task DecryptAsync_WithNullString_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _encryptionService.DecryptAsync(null);
        });
    }

    [Fact]
    public async Task DecryptAsync_WithInvalidBase64_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const string invalidCiphertext = "not-valid-base64!!!";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _encryptionService.DecryptAsync(invalidCiphertext);
        });
    }

    [Fact]
    public async Task DecryptAsync_WithTamperedCiphertext_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const string plaintext = "Original message";
        var ciphertext = await _encryptionService.EncryptAsync(plaintext);

        // Tamper with the ciphertext
        var ciphertextBytes = Convert.FromBase64String(ciphertext);
        if (ciphertextBytes.Length > 20)
        {
            ciphertextBytes[20] ^= 0xFF; // Flip bits
        }
        var tamperedCiphertext = Convert.ToBase64String(ciphertextBytes);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _encryptionService.DecryptAsync(tamperedCiphertext);
        });
    }

    [Fact]
    public async Task DecryptAsync_WithWrongKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const string plaintext = "Secret message";
        var ciphertext = await _encryptionService.EncryptAsync(plaintext);

        // Create a different key
        var differentKey = GenerateValidKey();
        var differentService = new EncryptionService(differentKey);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await differentService.DecryptAsync(ciphertext);
        });
    }

    [Fact]
    public async Task DecryptAsync_WithLongEncryptedText_ShouldHandleCorrectly()
    {
        // Arrange
        var longPlaintext = new string('X', 10000);
        var ciphertext = await _encryptionService.EncryptAsync(longPlaintext);

        // Act
        var decryptedText = await _encryptionService.DecryptAsync(ciphertext);

        // Assert
        Assert.Equal(longPlaintext, decryptedText);
    }

    [Fact]
    public async Task DecryptAsync_WithUnicodeEncryptedText_ShouldHandleCorrectly()
    {
        // Arrange
        const string plaintext = "Unicode: 你好 مرحبا 🔐";
        var ciphertext = await _encryptionService.EncryptAsync(plaintext);

        // Act
        var decryptedText = await _encryptionService.DecryptAsync(ciphertext);

        // Assert
        Assert.Equal(plaintext, decryptedText);
    }

    // ==================== GenerateKey Tests ====================

    [Fact]
    public void GenerateKey_ShouldReturnValidBase64String()
    {
        // Act
        var key = _encryptionService.GenerateKey();

        // Assert
        Assert.NotNull(key);
        Assert.NotEmpty(key);

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(key);
        Assert.NotEmpty(bytes);

        // Verify it's 32 bytes (256 bits for AES-256)
        Assert.Equal(32, bytes.Length);
    }

    [Fact]
    public void GenerateKey_ShouldGenerateDifferentKeysEachTime()
    {
        // Act
        var key1 = _encryptionService.GenerateKey();
        var key2 = _encryptionService.GenerateKey();

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_ShouldProduceValidEncryptionKey()
    {
        // Act
        var newKey = _encryptionService.GenerateKey();

        // Assert - Should be able to create a new service with the generated key
        var newService = new EncryptionService(newKey);
        Assert.NotNull(newService);
    }

    // ==================== GenerateIV Tests ====================

    [Fact]
    public void GenerateIV_ShouldReturnValidBase64String()
    {
        // Act
        var iv = _encryptionService.GenerateIV();

        // Assert
        Assert.NotNull(iv);
        Assert.NotEmpty(iv);

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(iv);
        Assert.NotEmpty(bytes);

        // Verify it's 16 bytes (128 bits for IV)
        Assert.Equal(16, bytes.Length);
    }

    [Fact]
    public void GenerateIV_ShouldGenerateDifferentIVsEachTime()
    {
        // Act
        var iv1 = _encryptionService.GenerateIV();
        var iv2 = _encryptionService.GenerateIV();

        // Assert
        Assert.NotEqual(iv1, iv2);
    }

    // ==================== Round-trip Tests ====================

    [Theory]
    [InlineData("Hello World")]
    [InlineData("123456789")]
    [InlineData("Special!@#$%^&*()")]
    [InlineData("")]
    public async Task RoundTrip_WithVariousInputs_ShouldSucceed(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return; // Skip empty string test

        // Act
        var ciphertext = await _encryptionService.EncryptAsync(plaintext);
        var decryptedText = await _encryptionService.DecryptAsync(ciphertext);

        // Assert
        Assert.Equal(plaintext, decryptedText);
    }

    [Fact]
    public async Task RoundTrip_WithMultipleMessages_ShouldSucceed()
    {
        // Arrange
        var messages = new[]
        {
            "First message",
            "Second message",
            "Third message",
            "Fourth message"
        };

        // Act & Assert
        foreach (var message in messages)
        {
            var ciphertext = await _encryptionService.EncryptAsync(message);
            var decryptedText = await _encryptionService.DecryptAsync(ciphertext);
            Assert.Equal(message, decryptedText);
        }
    }

    // ==================== CancellationToken Tests ====================

    [Fact]
    public async Task EncryptAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        const string plaintext = "Test message";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _encryptionService.EncryptAsync(plaintext, cts.Token);
        });
    }

    [Fact]
    public async Task DecryptAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        const string plaintext = "Test message";
        var ciphertext = await _encryptionService.EncryptAsync(plaintext);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _encryptionService.DecryptAsync(ciphertext, cts.Token);
        });
    }

    // ==================== Helper Methods ====================

    private static string GenerateValidKey()
    {
        using (var aes = System.Security.Cryptography.Aes.Create())
        {
            aes.KeySize = 256;
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
        }
    }
}
