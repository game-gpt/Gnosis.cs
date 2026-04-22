using System.Security.Cryptography;
using System.Text;

namespace Gnosis.Plugin.Manifest;

/// <summary>
/// 插件签名校验结果
/// </summary>
public sealed class SignatureValidationResult
{
    #region 属性

    /// <summary>
    /// 是否校验通过
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// 签名算法
    /// </summary>
    public string Algorithm { get; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; }

    #endregion

    #region 构造函数

    private SignatureValidationResult(bool isValid, string algorithm, string? errorMessage)
    {
        IsValid = isValid;
        Algorithm = algorithm;
        ErrorMessage = errorMessage;
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 创建校验通过的结果
    /// </summary>
    public static SignatureValidationResult Valid(string algorithm)
    {
        return new SignatureValidationResult(true, algorithm, null);
    }

    /// <summary>
    /// 创建校验失败的结果
    /// </summary>
    public static SignatureValidationResult Invalid(string algorithm, string errorMessage)
    {
        return new SignatureValidationResult(false, algorithm, errorMessage);
    }

    #endregion
}

/// <summary>
/// 插件签名校验器，验证插件清单和内容的完整性与真实性
/// </summary>
public sealed class SignatureValidator
{
    #region 字段

    private readonly Dictionary<string, byte[]> _trustedPublicKeys;

    #endregion

    #region 属性

    /// <summary>
    /// 签名验证模式
    /// </summary>
    public SignatureValidationMode Mode { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用验证模式初始化签名校验器
    /// </summary>
    /// <param name="mode">签名验证模式</param>
    public SignatureValidator(SignatureValidationMode mode = SignatureValidationMode.Optional)
    {
        Mode = mode;
        _trustedPublicKeys = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 添加受信任的公钥
    /// </summary>
    /// <param name="keyId">公钥标识</param>
    /// <param name="publicKey">公钥字节数据</param>
    public void AddTrustedPublicKey(string keyId, byte[] publicKey)
    {
        if (string.IsNullOrWhiteSpace(keyId))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(publicKey);

        _trustedPublicKeys[keyId] = publicKey;
    }

    /// <summary>
    /// 移除受信任的公钥
    /// </summary>
    /// <param name="keyId">公钥标识</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveTrustedPublicKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
        {
            return false;
        }

        return _trustedPublicKeys.Remove(keyId);
    }

    /// <summary>
    /// 验证插件清单签名
    /// </summary>
    /// <param name="manifest">插件清单</param>
    /// <returns>签名验证结果</returns>
    public SignatureValidationResult ValidateManifest(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (string.IsNullOrWhiteSpace(manifest.Signature))
        {
            return Mode switch
            {
                SignatureValidationMode.Required => SignatureValidationResult.Invalid(
                    "none",
                    $"插件 {manifest.Id} 未提供签名，但签名验证模式为必需"
                ),
                SignatureValidationMode.Optional => SignatureValidationResult.Valid("none"),
                SignatureValidationMode.Disabled => SignatureValidationResult.Valid("none"),
                _ => SignatureValidationResult.Valid("none")
            };
        }

        if (Mode == SignatureValidationMode.Disabled)
        {
            return SignatureValidationResult.Valid("skipped");
        }

        if (_trustedPublicKeys.Count == 0)
        {
            return SignatureValidationResult.Invalid(
                "unknown",
                $"插件 {manifest.Id} 包含签名，但没有配置受信任的公钥"
            );
        }

        var content = BuildManifestContent(manifest);
        var signatureBytes = Convert.FromBase64String(manifest.Signature);

        foreach (var (keyId, publicKey) in _trustedPublicKeys)
        {
            if (TryVerifySignature(content, signatureBytes, publicKey, "RSA-SHA256"))
            {
                return SignatureValidationResult.Valid("RSA-SHA256");
            }
        }

        return SignatureValidationResult.Invalid(
            "RSA-SHA256",
            $"插件 {manifest.Id} 签名验证失败，没有匹配的受信任公钥"
        );
    }

    /// <summary>
    /// 验证插件内容哈希
    /// </summary>
    /// <param name="data">插件内容数据</param>
    /// <param name="expectedHash">期望的 SHA-256 哈希值（Base64 编码）</param>
    /// <returns>是否验证通过</returns>
    public bool ValidateContentHash(byte[] data, string expectedHash)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return Mode != SignatureValidationMode.Required;
        }

        var actualHash = ComputeSha256Hash(data);
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 计算数据的 SHA-256 哈希值
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>SHA-256 哈希值（Base64 编码）</returns>
    public static string ComputeSha256Hash(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var hash = SHA256.HashData(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// 计算字符串的 SHA-256 哈希值
    /// </summary>
    /// <param name="content">字符串内容</param>
    /// <returns>SHA-256 哈希值（Base64 编码）</returns>
    public static string ComputeSha256Hash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return ComputeSha256Hash(bytes);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 构建清单签名内容（排除签名字段本身）
    /// </summary>
    private static string BuildManifestContent(PluginManifest manifest)
    {
        var sb = new StringBuilder();
        sb.Append(manifest.Id);
        sb.Append('|');
        sb.Append(manifest.Name);
        sb.Append('|');
        sb.Append(manifest.Version);
        sb.Append('|');
        sb.Append(manifest.EntryPoint);
        sb.Append('|');
        sb.Append(manifest.ApiVersion);

        foreach (var dep in manifest.Dependencies)
        {
            sb.Append('|');
            sb.Append(dep.PluginId);
            sb.Append('@');
            sb.Append(dep.VersionRange);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 尝试使用 RSA 公钥验证签名
    /// </summary>
    private static bool TryVerifySignature(
        string content,
        byte[] signature,
        byte[] publicKey,
        string algorithm)
    {
        try
        {
            using var rsa = RSA.Create();

            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);

            var data = Encoding.UTF8.GetBytes(content);
            var hashAlgorithm = algorithm switch
            {
                "RSA-SHA256" => HashAlgorithmName.SHA256,
                "RSA-SHA384" => HashAlgorithmName.SHA384,
                "RSA-SHA512" => HashAlgorithmName.SHA512,
                _ => HashAlgorithmName.SHA256
            };

            return rsa.VerifyData(data, signature, hashAlgorithm, RSASignaturePadding.Pkcs1);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    #endregion
}

/// <summary>
/// 签名验证模式
/// </summary>
public enum SignatureValidationMode
{
    /// <summary>
    /// 签名为必需，缺少签名则验证失败
    /// </summary>
    Required,

    /// <summary>
    /// 签名为可选，缺少签名则跳过验证
    /// </summary>
    Optional,

    /// <summary>
    /// 禁用签名验证
    /// </summary>
    Disabled
}
