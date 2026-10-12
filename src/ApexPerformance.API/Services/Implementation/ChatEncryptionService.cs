using System.Security.Cryptography;
using System.Text;
using ApexPerformance.API.Services.Interfaces;

namespace ApexPerformance.API.Services.Implementation;

/// <summary>
/// AES-256-GCM encryption of chat messages. The key comes from the
/// "ChatEncryptionKey" setting (base64, 32 bytes). Until it is set, a key
/// derived from the JWT signing key (HKDF) is used. Every message stores
/// the id of its key, so both keys keep working when the setting is added.
/// The conversation id is authenticated with the message, so encrypted
/// content can't be moved to another conversation.
/// </summary>
public class ChatEncryptionService : IChatEncryptionService
{
    private const string ConfiguredKeyId = "c1";
    private const string DerivedKeyId = "j1";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly Dictionary<string, byte[]> _keys = new();
    private readonly string _currentKeyId;
    private readonly ILogger<ChatEncryptionService> _logger;

    public ChatEncryptionService(IConfiguration configuration, ILogger<ChatEncryptionService> logger)
    {
        _logger = logger;

        var jwtKey = configuration["JWTSecretKey"];

        if (!string.IsNullOrEmpty(jwtKey))
            _keys[DerivedKeyId] = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(jwtKey), 32,
                info: Encoding.UTF8.GetBytes("apex-performance-chat-messages-v1"));

        var configuredKey = configuration["ChatEncryptionKey"];

        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            var key = Convert.FromBase64String(configuredKey);

            if (key.Length != 32)
                throw new InvalidOperationException("ChatEncryptionKey must be 32 bytes encoded as base64.");

            _keys[ConfiguredKeyId] = key;
        }

        _currentKeyId = _keys.ContainsKey(ConfiguredKeyId) ? ConfiguredKeyId : DerivedKeyId;

        if (!_keys.ContainsKey(_currentKeyId))
            throw new InvalidOperationException("No key for chat message encryption is configured.");
    }

    public (string EncryptedContent, string KeyId) Encrypt(string content, Guid conversationId)
    {
        var plain = Encoding.UTF8.GetBytes(content);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_keys[_currentKeyId], TagSize);
        aes.Encrypt(nonce, plain, cipher, tag, conversationId.ToByteArray());

        // nonce | tag | ciphertext
        var result = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSize);
        cipher.CopyTo(result, NonceSize + TagSize);

        return (Convert.ToBase64String(result), _currentKeyId);
    }

    public string? Decrypt(string encryptedContent, string keyId, Guid conversationId)
    {
        if (!_keys.TryGetValue(keyId, out var key))
        {
            _logger.LogError("Chat message key {KeyId} is not configured", keyId);
            return null;
        }

        try
        {
            var data = Convert.FromBase64String(encryptedContent);

            if (data.Length < NonceSize + TagSize)
                return null;

            var nonce = data.AsSpan(0, NonceSize);
            var tag = data.AsSpan(NonceSize, TagSize);
            var cipher = data.AsSpan(NonceSize + TagSize);
            var plain = new byte[cipher.Length];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, cipher, tag, plain, conversationId.ToByteArray());

            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            _logger.LogError(ex, "Chat message of conversation {ConversationId} could not be decrypted", conversationId);
            return null;
        }
    }
}
