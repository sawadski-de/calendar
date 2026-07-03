namespace Application.Sync;

/// <summary>
/// Symmetric encryption for OAuth tokens at rest (AD-8). Application defines the contract;
/// Infrastructure owns the concrete algorithm/key handling — Domain and Application never see a
/// plaintext token, and decryption is only ever called immediately before a provider API call.
/// </summary>
public interface ITokenEncryption
{
    string Encrypt(string plaintext);

    /// <summary>Throws if <paramref name="ciphertext"/> was tampered with or the key is wrong — never
    /// silently returns garbage (AES-GCM's authentication tag makes this detectable).</summary>
    string Decrypt(string ciphertext);
}
