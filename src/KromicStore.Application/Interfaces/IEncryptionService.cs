namespace KromicStore.Application.Interfaces;

/// <summary>
/// Interface for encryption services.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the specified plaintext using AES encryption.
    /// </summary>
    /// <param name="plaintext">The plaintext to encrypt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The encrypted ciphertext (Base64 encoded).</returns>
    Task<string> EncryptAsync(string plaintext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts the specified ciphertext using AES decryption.
    /// </summary>
    /// <param name="ciphertext">The Base64 encoded ciphertext to decrypt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decrypted plaintext.</returns>
    Task<string> DecryptAsync(string ciphertext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new encryption key.
    /// </summary>
    /// <returns>The generated key (Base64 encoded).</returns>
    string GenerateKey();

    /// <summary>
    /// Generates a new initialization vector (IV).
    /// </summary>
    /// <returns>The generated IV (Base64 encoded).</returns>
    string GenerateIV();
}
