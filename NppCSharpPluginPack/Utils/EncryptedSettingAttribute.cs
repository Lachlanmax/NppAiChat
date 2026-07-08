using System;

namespace NppAiChat.Utils;

/// <summary>
/// Marks a settings property so that its value is encrypted with DPAPI
/// before being written to the INI file and decrypted when read back.
/// Apply this attribute to any setting that holds secrets (e.g. API tokens).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EncryptedSettingAttribute : Attribute
{
}
