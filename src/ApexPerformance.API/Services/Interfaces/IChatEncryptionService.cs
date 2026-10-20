namespace ApexPerformance.API.Services.Interfaces;

public interface IChatEncryptionService
{
    /// <summary>
    /// Encrypts message content, bound to the conversation it belongs to.
    /// </summary>
    (string EncryptedContent, string KeyId) Encrypt(string content, Guid conversationId);

    /// <summary>
    /// Decrypted content, or null when it can't be decrypted (unknown key, changed data).
    /// </summary>
    string? Decrypt(string encryptedContent, string keyId, Guid conversationId);

    /// <summary>
    /// Encrypts an attachment (picture) of the conversation.
    /// </summary>
    (byte[] EncryptedData, string KeyId) EncryptBytes(byte[] data, Guid conversationId);

    /// <summary>
    /// Decrypted attachment, or null when it can't be decrypted.
    /// </summary>
    byte[]? DecryptBytes(byte[] encryptedData, string keyId, Guid conversationId);
}
