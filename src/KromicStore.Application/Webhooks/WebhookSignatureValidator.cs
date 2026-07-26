namespace KromicStore.Application.Webhooks;

using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Helper class for webhook consumers to verify webhook signatures and timestamps.
/// Provides static methods to validate webhook authenticity.
/// </summary>
public static class WebhookSignatureValidator
{
    /// <summary>
    /// The timestamp tolerance window in minutes (5 minutes by default).
    /// </summary>
    public const int TimestampToleranceMinutes = 5;

    /// <summary>
    /// Verifies the webhook signature and timestamp.
    /// </summary>
    /// <param name="payload">The raw request body (JSON string).</param>
    /// <param name="signature">The value of X-KromicStore-Signature header.</param>
    /// <param name="timestamp">The value of X-KromicStore-Timestamp header (ISO 8601 format).</param>
    /// <param name="secret">The webhook secret (Base64-encoded).</param>
    /// <param name="tolerance">Optional custom timestamp tolerance in minutes.</param>
    /// <returns>True if signature is valid and timestamp is within tolerance; false otherwise.</returns>
    public static bool VerifySignature(
        string payload,
        string signature,
        string timestamp,
        string secret,
        int tolerance = TimestampToleranceMinutes)
    {
        try
        {
            // Validate timestamp to prevent replay attacks
            if (!ValidateTimestamp(timestamp, tolerance))
            {
                return false;
            }

            // Validate signature format
            if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256="))
            {
                return false;
            }

            // Extract the signature value (remove "sha256=" prefix)
            string providedSignature = signature.Substring(7);

            // Generate expected signature
            string expectedSignature = ComputeSignature(payload, secret);

            // Use constant-time comparison to prevent timing attacks
            return ConstantTimeEquals(providedSignature, expectedSignature);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that the webhook timestamp is within the tolerance window.
    /// </summary>
    /// <param name="timestamp">The timestamp from X-KromicStore-Timestamp header (ISO 8601 format).</param>
    /// <param name="tolerance">Tolerance in minutes.</param>
    /// <returns>True if timestamp is within tolerance; false otherwise.</returns>
    public static bool ValidateTimestamp(string timestamp, int tolerance = TimestampToleranceMinutes)
    {
        try
        {
            if (!DateTime.TryParse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var timestampUtc))
            {
                return false;
            }

            var now = DateTime.UtcNow;
            var age = now - timestampUtc;

            // Check if timestamp is too old or in the future
            if (age.TotalMinutes > tolerance || age.TotalSeconds < -1)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Computes the HMAC-SHA256 signature for a payload.
    /// </summary>
    /// <param name="payload">The JSON payload.</param>
    /// <param name="secret">The webhook secret (Base64-encoded).</param>
    /// <returns>Hex-encoded HMAC-SHA256 signature.</returns>
    public static string ComputeSignature(string payload, string secret)
    {
        try
        {
            // Decode Base64 secret
            byte[] secretBytes = Convert.FromBase64String(secret);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

            // Generate HMAC-SHA256
            using (var hmac = new HMACSHA256(secretBytes))
            {
                byte[] hash = hmac.ComputeHash(payloadBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        catch
        {
            throw new InvalidOperationException("Failed to compute signature");
        }
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks.
    /// </summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return false;
        }

        if (a.Length != b.Length)
        {
            return false;
        }

        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        return diff == 0;
    }
}
