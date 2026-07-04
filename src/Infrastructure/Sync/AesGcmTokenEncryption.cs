using System.Security.Cryptography;
using Application.Sync;

namespace Infrastructure.Sync;

/// <summary>
/// AES-256-GCM implementation of <see cref="ITokenEncryption"/> (AD-8). Stored format is
/// base64(nonce[12] || tag[16] || ciphertext) — a single opaque string so <see cref="Domain.CalendarConnection"/>
/// needs only one column per token, and GCM's authentication tag means a tampered/corrupted value
/// throws on decrypt instead of silently returning garbage.
/// </summary>
public class AesGcmTokenEncryption : ITokenEncryption
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _key;

    public AesGcmTokenEncryption(TokenEncryptionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Base64Key))
        {
            throw new InvalidOperationException(
                "TOKEN_ENCRYPTION_KEY is not set. Set a base64-encoded 32-byte key via the TOKEN_ENCRYPTION_KEY " +
                "environment variable before starting this process (AD-8) — see deploy/.env.example.");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(options.Base64Key);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("TOKEN_ENCRYPTION_KEY is not valid base64.", ex);
        }

        if (key.Length != 32)
        {
            throw new InvalidOperationException(
                $"TOKEN_ENCRYPTION_KEY must decode to exactly 32 bytes for AES-256-GCM, got {key.Length}.");
        }

        _key = key;
    }

    public string Encrypt(string plaintext)
    {
        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(_key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, result, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSizeBytes + TagSizeBytes, ciphertext.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        var raw = Convert.FromBase64String(ciphertext);
        if (raw.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Ciphertext is too short to contain a nonce and tag.");
        }

        var nonce = raw[..NonceSizeBytes];
        var tag = raw[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
        var encrypted = raw[(NonceSizeBytes + TagSizeBytes)..];
        var plaintextBytes = new byte[encrypted.Length];

        using var aesGcm = new AesGcm(_key, TagSizeBytes);
        // Throws CryptographicException if the tag doesn't match — a tampered ciphertext or wrong key
        // is detected here rather than silently producing garbage plaintext.
        aesGcm.Decrypt(nonce, encrypted, tag, plaintextBytes);
        return System.Text.Encoding.UTF8.GetString(plaintextBytes);
    }
}

/// <summary>Bound from the <c>TOKEN_ENCRYPTION_KEY</c> environment variable in both Api and Worker composition roots.</summary>
public class TokenEncryptionOptions
{
    public string Base64Key { get; set; } = string.Empty;
}
