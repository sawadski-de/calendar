using System.Security.Cryptography;
using Infrastructure.Sync;
using Xunit;

namespace UnitTests.Infrastructure;

public class AesGcmTokenEncryptionTests
{
    private static readonly string ValidKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Fact]
    public void Encrypt_then_Decrypt_round_trips_the_original_plaintext()
    {
        var sut = new AesGcmTokenEncryption(new TokenEncryptionOptions { Base64Key = ValidKey });

        var ciphertext = sut.Encrypt("ya29.a0AfH6SMB...");

        Assert.Equal("ya29.a0AfH6SMB...", sut.Decrypt(ciphertext));
    }

    [Fact]
    public void Encrypt_never_returns_the_plaintext_verbatim()
    {
        var sut = new AesGcmTokenEncryption(new TokenEncryptionOptions { Base64Key = ValidKey });

        var ciphertext = sut.Encrypt("super-secret-refresh-token");

        Assert.DoesNotContain("super-secret-refresh-token", ciphertext);
    }

    [Fact]
    public void Decrypt_throws_instead_of_returning_garbage_when_ciphertext_is_tampered_with()
    {
        var sut = new AesGcmTokenEncryption(new TokenEncryptionOptions { Base64Key = ValidKey });
        var ciphertextBytes = Convert.FromBase64String(sut.Encrypt("token-value"));

        // Flip one byte inside the actual ciphertext region (past the 12-byte nonce + 16-byte tag) —
        // AES-GCM's authentication tag must catch this rather than silently decrypting garbage.
        ciphertextBytes[^1] ^= 0xFF;
        var tamperedCiphertext = Convert.ToBase64String(ciphertextBytes);

        Assert.Throws<AuthenticationTagMismatchException>(() => sut.Decrypt(tamperedCiphertext));
    }

    [Fact]
    public void Decrypt_throws_when_the_key_does_not_match_the_one_used_to_encrypt()
    {
        var encryptor = new AesGcmTokenEncryption(new TokenEncryptionOptions { Base64Key = ValidKey });
        var ciphertext = encryptor.Encrypt("token-value");

        var otherKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var decryptorWithWrongKey = new AesGcmTokenEncryption(new TokenEncryptionOptions { Base64Key = otherKey });

        Assert.ThrowsAny<CryptographicException>(() => decryptorWithWrongKey.Decrypt(ciphertext));
    }

    [Fact]
    public void Constructor_throws_a_clear_error_when_TOKEN_ENCRYPTION_KEY_is_missing()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new AesGcmTokenEncryption(new TokenEncryptionOptions { Base64Key = "" }));
        Assert.Contains("TOKEN_ENCRYPTION_KEY", ex.Message);
    }

    [Fact]
    public void Constructor_throws_when_the_key_is_not_valid_base64()
    {
        Assert.Throws<InvalidOperationException>(() => new AesGcmTokenEncryption(new TokenEncryptionOptions { Base64Key = "not-base64!!!" }));
    }

    [Fact]
    public void Constructor_throws_when_the_decoded_key_is_not_32_bytes()
    {
        var tooShortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        Assert.Throws<InvalidOperationException>(() => new AesGcmTokenEncryption(new TokenEncryptionOptions { Base64Key = tooShortKey }));
    }
}
