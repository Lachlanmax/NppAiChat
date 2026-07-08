using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace NppAiChat.Utils;

public static class DpapiHelper
{
    public const string PREFIX = "DPAPI:";

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return PREFIX + Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DpapiHelper.Encrypt failed: {ex.Message}");
            return string.Empty;
        }
    }

    public static string Decrypt(string encryptedPrefixed)
    {
        if (string.IsNullOrEmpty(encryptedPrefixed))
            return string.Empty;

        string base64 = encryptedPrefixed.StartsWith(PREFIX)
            ? encryptedPrefixed.Substring(PREFIX.Length)
            : encryptedPrefixed;

        if (string.IsNullOrEmpty(base64))
            return string.Empty;

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(base64);
            byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DpapiHelper.Decrypt failed: {ex.Message}");
            return string.Empty;
        }
    }

    public static bool IsEncrypted(string value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith(PREFIX);
    }
}
