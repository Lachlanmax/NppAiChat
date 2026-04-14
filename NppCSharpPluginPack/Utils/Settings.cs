using System.ComponentModel;
using NppAiChat.PluginInfrastructure;

namespace NppAiChat.Utils;

/// <summary>
/// Manages application settings
/// </summary>
public class Settings : SettingsBase
{
    #region LLM
    [Description("LLM API endpoint (e.g. https://models.github.ai/inference/chat/completions)"),
     Category("LLM"), DefaultValue("")]
    public string llm_endpoint { get; set; } = "";

    [Description("LLM API bearer token (stored in plugin settings)"),
     Category("LLM"), DefaultValue("")]
    public string llm_token { get; set; } = "";

    [Description("LLM model name (e.g. openai/gpt-4.1)"),
     Category("LLM"), DefaultValue("")]
    public string llm_model { get; set; } = "";
    #endregion
}