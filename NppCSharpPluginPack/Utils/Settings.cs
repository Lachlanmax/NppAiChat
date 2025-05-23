using System.ComponentModel;
using CsvQuery.PluginInfrastructure;
using Kbg.NppPluginNET;

namespace NppDemo.Utils
{
    /// <summary>
    /// Manages application settings
    /// </summary>
    public class Settings : SettingsBase
    {
        /// <inheritdoc />
        public override void OnSettingsChanged()
        {
            base.OnSettingsChanged();
            Main.RestyleEverything();
        }

        #region LLM
        [Description("LLM API endpoint (e.g. https://models.github.ai/inference/chat/completions)"),
         Category("LLM"), DefaultValue("")]
        public string llm_endpoint { get; set; } = "";

        [Description("LLM API bearer token (stored in plugin settings)"),
         Category("LLM"), DefaultValue("")]
        public string llm_token { get; set; } = "";

        [Description("LLM model name (e.g. openai/gpt-4.1)"),
         Category("LLM"), DefaultValue("openai/gpt-4.1")]
        public string llm_model { get; set; } = "openai/gpt-4.1";

        [Description("Include current editor file content as context in chat messages"),
         Category("LLM"), DefaultValue(true)]
        public bool llm_include_editor_context { get; set; } = true;
        #endregion
    }
}
