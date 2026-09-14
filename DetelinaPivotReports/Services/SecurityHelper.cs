using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DetelinaPivotReports.Services;

/// <summary>
/// Помощен клас за сигурно двупосочно криптиране на пароли за базата данни.
/// За разлика от еднопосочния хеш, симетричното AES-256 криптиране позволява на приложението
/// да декриптира паролата в оперативната памет за изграждане на Firebird връзката,
/// докато във външния файл (appsettings.json) паролата остава защитена и нечетима.
/// </summary>
public static class SecurityHelper
{
    private const string EncryptedPrefix = "enc:";

    // Фиксиран 256-битов (32 байта) ключ за приложението
    private static readonly byte[] EncryptionKey = new byte[]
    {
        0x44, 0x65, 0x74, 0x65, 0x6C, 0x69, 0x6E, 0x61, // "Detelina"
        0x50, 0x69, 0x76, 0x6F, 0x74, 0x32, 0x30, 0x32, // "Pivot202"
        0x36, 0x40, 0x45, 0x6C, 0x74, 0x72, 0x61, 0x64, // "6@Eltrad"
        0x65, 0x24, 0x41, 0x54, 0x4D, 0x21, 0x23, 0x2A  // "e$ATM!#*"
    };

    /// <summary>
    /// Криптира подадения чист текст с AES-256 (с нов случаен IV за всеки запис)
    /// и връща Base64 низ с префикс "enc:".
    /// </summary>
    public static string EncryptPassword(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        // Ако вече е криптиран низ, не го прекриптираме отново
        if (plainText.StartsWith(EncryptedPrefix, StringComparison.OrdinalIgnoreCase))
            return plainText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.GenerateIV();

            using var ms = new MemoryStream();
            // Записваме първите 16 байта (IV)
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            byte[] cipherBytes = ms.ToArray();
            return EncryptedPrefix + Convert.ToBase64String(cipherBytes);
        }
        catch
        {
            // При неочаквана грешка връщаме оригиналния текст
            return plainText;
        }
    }

    /// <summary>
    /// Декриптира подадения криптиран низ с префикс "enc:".
    /// Ако низът е в обикновен текст (стар формат без "enc:"), го връща директно без промяна.
    /// </summary>
    public static string DecryptPassword(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        // Обратна съвместимост: ако не започва с "enc:", значи е записан в чист текст
        if (!cipherText.StartsWith(EncryptedPrefix, StringComparison.OrdinalIgnoreCase))
            return cipherText;

        try
        {
            string base64 = cipherText.Substring(EncryptedPrefix.Length);
            byte[] allBytes = Convert.FromBase64String(base64);

            using var aes = Aes.Create();
            aes.Key = EncryptionKey;

            int ivLength = aes.BlockSize / 8; // 16 байта за AES
            if (allBytes.Length < ivLength)
                return string.Empty;

            byte[] iv = new byte[ivLength];
            Array.Copy(allBytes, 0, iv, 0, ivLength);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(allBytes, ivLength, allBytes.Length - ivLength);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);

            return sr.ReadToEnd();
        }
        catch
        {
            // При грешка при декриптиране (напр. повредена конфигурация) връщаме празен низ
            return string.Empty;
        }
    }
}
