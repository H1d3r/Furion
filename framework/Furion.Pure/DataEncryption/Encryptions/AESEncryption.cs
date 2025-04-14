// ------------------------------------------------------------------------
// 版权信息
// 版权归百小僧及百签科技（广东）有限公司所有。
// 所有权利保留。
// 官方网站：https://baiqian.com
//
// 许可证信息
// Furion 项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。
// 许可证的完整文本可以在源代码树根目录中的 LICENSE-APACHE 和 LICENSE-MIT 文件中找到。
// 官方网站：https://furion.net
//
// 使用条款
// 使用本代码应遵守相关法律法规和许可证的要求。
//
// 免责声明
// 对于因使用本代码而产生的任何直接、间接、偶然、特殊或后果性损害，我们不承担任何责任。
//
// 其他重要信息
// Furion 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。
// 有关 Furion 项目的其他详细信息，请参阅位于源代码树根目录中的 COPYRIGHT 和 DISCLAIMER 文件。
//
// 更多信息
// 请访问 https://gitee.com/dotnetchina/Furion 获取更多关于 Furion 项目的许可证和版权信息。
// ------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Furion.DataEncryption;

/// <summary>
/// AES 加解密
/// </summary>
[SuppressSniffer]
public class AESEncryption
{
    /// <summary>
    /// 加密
    /// </summary>
    /// <param name="text">加密文本</param>
    /// <param name="skey">密钥</param>
    /// <param name="iv">偏移量</param>
    /// <param name="mode">模式</param>
    /// <param name="padding">填充</param>
    /// <param name="isBase64"></param>
    /// <returns></returns>
    public static string Encrypt(string text, string skey, byte[] iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, bool isBase64 = false)
    {
        var bKey = !isBase64 ? Encoding.UTF8.GetBytes(skey) : Convert.FromBase64String(skey);
        if (bKey.Length != 16 && bKey.Length != 24 && bKey.Length != 32) throw new ArgumentException("The key length must be 16, 24, or 32 bytes.");

        using var aesAlg = Aes.Create();
        aesAlg.Key = bKey;
        aesAlg.Mode = mode;
        aesAlg.Padding = padding;

        if (mode != CipherMode.ECB)
        {
            aesAlg.IV = iv ?? aesAlg.IV;
            if (iv != null && iv.Length != 16) throw new ArgumentException("The IV length must be 16 bytes.");
        }

        using var encryptor = aesAlg.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
        {
            swEncrypt.Write(text);
        }

        var encryptedContent = msEncrypt.ToArray();

        // 仅在未提供 IV 时拼接 IV
        if (mode != CipherMode.ECB && iv == null)
        {
            var result = new byte[aesAlg.IV.Length + encryptedContent.Length];
            Buffer.BlockCopy(aesAlg.IV, 0, result, 0, aesAlg.IV.Length);
            Buffer.BlockCopy(encryptedContent, 0, result, aesAlg.IV.Length, encryptedContent.Length);
            return Convert.ToBase64String(result);
        }

        // 如果是 ECB 模式，直接返回密文的 Base64 编码
        return Convert.ToBase64String(encryptedContent);
    }

    /// <summary>
    /// 解密
    /// </summary>
    /// <param name="hash">加密后字符串</param>
    /// <param name="skey">密钥</param>
    /// <param name="iv">偏移量</param>
    /// <param name="mode">模式</param>
    /// <param name="padding">填充</param>
    /// <param name="isBase64"></param>
    /// <returns></returns>
    public static string Decrypt(string hash, string skey, byte[] iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, bool isBase64 = false)
    {
        var fullCipher = Convert.FromBase64String(hash);
        var bKey = !isBase64 ? Encoding.UTF8.GetBytes(skey) : Convert.FromBase64String(skey);
        if (bKey.Length != 16 && bKey.Length != 24 && bKey.Length != 32) throw new ArgumentException("The key length must be 16, 24, or 32 bytes.");

        using var aesAlg = Aes.Create();
        aesAlg.Key = bKey;
        aesAlg.Mode = mode;
        aesAlg.Padding = padding;

        if (mode != CipherMode.ECB)
        {
            if (iv == null)
            {
                if (fullCipher.Length < aesAlg.BlockSize / 8) throw new ArgumentException("The ciphertext length is insufficient to extract the IV.");

                iv = new byte[aesAlg.BlockSize / 8];
                var cipher = new byte[fullCipher.Length - iv.Length];
                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);
                aesAlg.IV = iv;
                fullCipher = cipher;
            }
            else
            {
                if (iv.Length != 16) throw new ArgumentException("The IV length must be 16 bytes.");
                aesAlg.IV = iv;
            }
        }

        using var decryptor = aesAlg.CreateDecryptor();
        using var msDecrypt = new MemoryStream(fullCipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8);

        return srDecrypt.ReadToEnd();
    }

    /// <summary>
    /// 加密
    /// </summary>
    /// <param name="bytes">源文件 字节数组</param>
    /// <param name="skey">密钥</param>
    /// <param name="iv">偏移量</param>
    /// <param name="mode">模式</param>
    /// <param name="padding">填充</param>
    /// <param name="isBase64"></param>
    /// <returns>加密后的字节数组</returns>
    public static byte[] Encrypt(byte[] bytes, string skey, byte[] iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, bool isBase64 = false)
    {
        // 验证密钥长度
        var bKey = !isBase64 ? Encoding.UTF8.GetBytes(skey) : Convert.FromBase64String(skey);
        if (bKey.Length != 16 && bKey.Length != 24 && bKey.Length != 32) throw new ArgumentException("The key length must be 16, 24, or 32 bytes.");

        using var aesAlg = Aes.Create();
        aesAlg.Key = bKey;
        aesAlg.Mode = mode;
        aesAlg.Padding = padding;

        if (mode != CipherMode.ECB)
        {
            aesAlg.IV = iv ?? GenerateRandomIV();
            if (aesAlg.IV.Length != 16) throw new ArgumentException("The IV length must be 16 bytes.");
        }

        using var memoryStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(memoryStream, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();
        }

        var encryptedContent = memoryStream.ToArray();

        // 仅在未提供 IV 时拼接 IV
        if (mode != CipherMode.ECB && iv == null)
        {
            var result = new byte[aesAlg.IV.Length + encryptedContent.Length];
            Buffer.BlockCopy(aesAlg.IV, 0, result, 0, aesAlg.IV.Length);
            Buffer.BlockCopy(encryptedContent, 0, result, aesAlg.IV.Length, encryptedContent.Length);
            return result;
        }

        return encryptedContent;
    }

    /// <summary>
    /// 解密
    /// </summary>
    /// <param name="bytes">加密后文件 字节数组</param>
    /// <param name="skey">密钥</param>
    /// <param name="iv">偏移量</param>
    /// <param name="mode">模式</param>
    /// <param name="padding">填充</param>
    /// <param name="isBase64"></param>
    /// <returns></returns>
    public static byte[] Decrypt(byte[] bytes, string skey, byte[] iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, bool isBase64 = false)
    {
        // 验证密钥长度
        var bKey = !isBase64 ? Encoding.UTF8.GetBytes(skey) : Convert.FromBase64String(skey);
        if (bKey.Length != 16 && bKey.Length != 24 && bKey.Length != 32) throw new ArgumentException("The key length must be 16, 24, or 32 bytes.");

        using var aesAlg = Aes.Create();
        aesAlg.Key = bKey;
        aesAlg.Mode = mode;
        aesAlg.Padding = padding;

        if (mode != CipherMode.ECB)
        {
            if (iv == null)
            {
                // 提取IV
                if (bytes.Length < 16) throw new ArgumentException("The ciphertext length is insufficient to extract the IV.");
                iv = bytes.Take(16).ToArray();
                bytes = bytes.Skip(16).ToArray();
            }
            else
            {
                if (iv.Length != 16) throw new ArgumentException("The IV length must be 16 bytes.");
            }
            aesAlg.IV = iv;
        }

        using var memoryStream = new MemoryStream(bytes);
        using var cryptoStream = new CryptoStream(memoryStream, aesAlg.CreateDecryptor(), CryptoStreamMode.Read);
        using var originalStream = new MemoryStream();

        cryptoStream.CopyTo(originalStream);
        return originalStream.ToArray();
    }

    /// <summary>
    /// 生成随机 IV
    /// </summary>
    /// <returns></returns>
    private static byte[] GenerateRandomIV()
    {
        using var aes = Aes.Create();
        aes.GenerateIV();
        return aes.IV;
    }
}