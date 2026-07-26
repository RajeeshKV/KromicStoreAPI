namespace KromicStore.Infrastructure.Services;

using Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// AES-based encryption service implementation.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly string _encryptionKey;

    /// <summary>
    /// Initializes a new instance of the EncryptionService class.
    /// </summary>
    /// <param name="encryptionKey">The Base64 encoded encryption key (must be 32 bytes for AES-256).</param>
    /// <exception cref="ArgumentException">Thrown when the encryption key is invalid.</exception>
    public EncryptionService(string encryptionKey)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new ArgumentException("Encryption key cannot be null or empty.", nameof(encryptionKey));

        try
        {
            var key = Convert.FromBase64String(encryptionKey);
            if (key.Length != 32)
                throw new ArgumentException("Encryption key must be exactly 32 bytes (for AES-256).", nameof(encryptionKey));
        }
        catch (FormatException)
        {
            throw new ArgumentException("Encryption key must be a valid Base64 string.", nameof(encryptionKey));
        }

        _encryptionKey = encryptionKey;
    }

    /// <inheritdoc />
    public Task<string> EncryptAsync(string plaintext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException("Plaintext cannot be null or empty.", nameof(plaintext));

        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(_encryptionKey);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    using (var ms = new MemoryStream())
                    {
                        // Write the IV to the beginning of the stream
                        ms.Write(aes.IV, 0, aes.IV.Length);

                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                            cs.Write(plaintextBytes, 0, plaintextBytes.Length);
                            cs.FlushFinalBlock();
                        }

                        var ciphertextBytes = ms.ToArray();
                        return Task.FromResult(Convert.ToBase64String(ciphertextBytes));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Encryption failed.", ex);
        }
    }

    /// <inheritdoc />
    public Task<string> DecryptAsync(string ciphertext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(ciphertext))
            throw new ArgumentException("Ciphertext cannot be null or empty.", nameof(ciphertext));

        try
        {
            var ciphertextBytes = Convert.FromBase64String(ciphertext);

            using (var aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(_encryptionKey);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV from the beginning of the ciphertext
                var iv = new byte[aes.IV.Length];
                Array.Copy(ciphertextBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    using (var ms = new MemoryStream(ciphertextBytes, iv.Length, ciphertextBytes.Length - iv.Length))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var reader = new StreamReader(cs, Encoding.UTF8))
                            {
                                var plaintext = reader.ReadToEnd();
                                return Task.FromResult(plaintext);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Decryption failed.", ex);
        }
    }

    /// <inheritdoc />
    public string GenerateKey()
    {
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256; // AES-256
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
        }
    }

    /// <inheritdoc />
    public string GenerateIV()
    {
        using (var aes = Aes.Create())
        {
            aes.GenerateIV();
            return Convert.ToBase64String(aes.IV);
        }
    }
}
