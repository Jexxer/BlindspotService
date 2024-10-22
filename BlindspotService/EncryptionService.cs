using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Blindspot.Services;

public class EncryptionService
{
    // Example hard-coded encryption key and IV.
    private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("x9H3lA4bT2sQr8Z6"); // 16 bytes for AES-128
    private static readonly byte[] EncryptionIV = Encoding.UTF8.GetBytes("m3Fj5Lp9Qw7Rt1Y4"); // 16 bytes for AES-128

    public string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = EncryptionKey;
            aes.IV = EncryptionIV;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);

                // Ensure all data is written to the underlying stream
                swEncrypt.Flush();
                csEncrypt.FlushFinalBlock(); // this is important when writing to a CryptoStream

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    public string Decrypt(string encryptedText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = EncryptionKey;
            aes.IV = EncryptionIV;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedText)))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }
}
