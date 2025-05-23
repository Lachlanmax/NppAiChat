using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NppDemo.Utils;
using Kbg.NppPluginNET.PluginInfrastructure;
using Kbg.NppPluginNET;

namespace NppDemo.Forms
{
    public class ChatDockForm : FormBase
    {
        private RichTextBox chatHistory;
        private TextBox inputBox;
        private Button sendBtn;
        private Button applyEditBtn;
        private CheckBox includeContextCheckBox;
        private CheckBox autoApplyCheckBox;
        private string lastAssistantText = "";
        private List<ChatMessage> conversationHistory = new List<ChatMessage>();

        public ChatDockForm() : base(false, true)
        {
            Text = "Assistant";
            ClientSize = new Size(500, 800);
            BackColor = SystemColors.ControlDarkDark;

            // Button panel (at the very bottom)
            var panel = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 45, 
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5),
                BackColor = SystemColors.ControlDarkDark
            };

            sendBtn = new Button 
            { 
                Text = "Send", 
                Width = 80,
                Height = 35,
                BackColor = SystemColors.Highlight,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            applyEditBtn = new Button 
            { 
                Text = "Apply as Edit", 
                Width = 130,
                Height = 35,
                BackColor = SystemColors.GrayText,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9)
            };
            var clearBtn = new Button 
            { 
                Text = "Clear History", 
                Width = 130,
                Height = 35,
                BackColor = SystemColors.ControlDarkDark,
                ForeColor = SystemColors.ControlText,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9)
            };

            panel.Controls.Add(sendBtn);
            panel.Controls.Add(applyEditBtn);
            panel.Controls.Add(clearBtn);

            // Checkbox container (above input)
            var checkboxContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(5),
                BackColor = SystemColors.ControlDarkDark
            };

            var checkboxPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = false
            };

            includeContextCheckBox = new CheckBox
            {
                Text = "Include editor context",
                Checked = Main.settings.llm_include_editor_context,
                AutoSize = true,
                ForeColor = SystemColors.ControlText,
                BackColor = SystemColors.ControlDarkDark
            };
            
            autoApplyCheckBox = new CheckBox
            {
                Text = "Auto-apply code edits",
                Checked = false,
                AutoSize = true,
                ForeColor = SystemColors.ControlText,
                BackColor = SystemColors.ControlDarkDark
            };

            checkboxPanel.Controls.Add(includeContextCheckBox);
            checkboxPanel.Controls.Add(autoApplyCheckBox);
            checkboxContainer.Controls.Add(checkboxPanel);

            // Input box container (above checkbox)
            var inputContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                Padding = new Padding(5),
                BackColor = SystemColors.ControlDarkDark
            };

            inputBox = new TextBox 
            { 
                Multiline = true,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.ControlText,
                BorderStyle = BorderStyle.Fixed3D,
                AcceptsTab = false,
                WordWrap = true
            };
            inputContainer.Controls.Add(inputBox);

            // Chat history display (fills the rest)
            chatHistory = new RichTextBox 
            { 
                Multiline = true, 
                ReadOnly = true, 
                ScrollBars = RichTextBoxScrollBars.Vertical, 
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.ControlText,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(8)
            };

            // Add controls in bottom-to-top order for proper docking
            Controls.Add(chatHistory);
            Controls.Add(panel);
            Controls.Add(checkboxContainer);
            Controls.Add(inputContainer);

            sendBtn.Click += async (s, e) => await SendPromptAsync();
            applyEditBtn.Click += (s, e) => ApplyAssistantAsEdit();
            clearBtn.Click += (s, e) => ClearConversationHistory();

            Translator.TranslateForm(this);
        }

        private static string GetCurrentFileContent(int maxChars = 30_000)
        {
            if (Npp.editor == null)
            {
                return string.Empty;
            }

            long length = Npp.editor.GetLength();
            long charsToRead = Math.Min(length, maxChars);

            var textRange = new TextRange(0, (int)charsToRead, (int)charsToRead + 1);
            Npp.editor.GetTextRange(textRange);
            string txt = textRange.lpstrText ?? string.Empty;
            if (length > maxChars)
            {
                txt += "\n...[truncated]";
            }

            return txt;
        }

        private async Task SendPromptAsync()
        {
            var prompt = inputBox.Text.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            AppendToHistoryFormatted("User", prompt, Color.FromArgb(0, 100, 0));
            inputBox.Clear();

            string endpoint = Main.settings.llm_endpoint;
            string token = Main.settings.llm_token;
            string model = Main.settings.llm_model;

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(token))
            {
                AppendToHistoryFormatted("Assistant", "Error - LLM endpoint or token not configured. Please check settings.", Color.Red);
                return;
            }

            AppendToHistoryFormatted("Assistant", "(thinking...)", SystemColors.GrayText);

            try
            {
                // Get editor content if context is enabled
                string editorContent = null;
                if (includeContextCheckBox.Checked)
                {
                    editorContent = GetCurrentFileContent();
                }

                string response = await LlmClient.SendPromptAsync(
                    endpoint, 
                    token, 
                    model,
                    prompt, 
                    conversationHistory,
                    editorContent,
                    includeContextCheckBox.Checked
                );

                // Extract explanation and code block if present
                var (explanation, codeBlock, shouldAutoApply) = LlmClient.ExtractCodeBlock(response);

                // Update last assistant text for applying edits
                lastAssistantText = codeBlock ?? response;

                // Display the response
                string displayText = explanation;
                bool isLargeCode = codeBlock != null && codeBlock.Length > 1000;
                
                if (!string.IsNullOrEmpty(codeBlock) && !isLargeCode)
                {
                    displayText += "\n\n[Code to apply]:\n" + codeBlock;
                }
                else if (isLargeCode)
                {
                    displayText += "\n\n[Large code block - will be applied automatically if enabled]";
                }

                // Remove the "(thinking...)" message and add actual response
                RemoveLastLine();
                AppendToHistoryFormatted("Assistant", displayText, Color.FromArgb(0, 0, 180));

                // Add to conversation history
                conversationHistory.Add(new ChatMessage("user", prompt));
                conversationHistory.Add(new ChatMessage("assistant", response));

                // Auto-apply code if enabled by user AND LLM marked it as auto-apply
                if (autoApplyCheckBox.Checked && shouldAutoApply && !string.IsNullOrEmpty(lastAssistantText))
                {
                    ApplyCodeAutomatically();
                }
            }
            catch (Exception ex)
            {
                RemoveLastLine();
                AppendToHistoryFormatted("Assistant", "(error) " + ex.Message, Color.Red);
            }
        }

        private void AppendToHistoryFormatted(string speaker, string message, Color color)
        {
            if (chatHistory.InvokeRequired)
            {
                chatHistory.Invoke(new Action(() => AppendToHistoryFormatted(speaker, message, color)));
                return;
            }

            // Add speaker label
            chatHistory.AppendText(speaker + ": ");
            int startIndex = chatHistory.TextLength - (speaker.Length + 2);
            chatHistory.Select(startIndex, speaker.Length + 1);
            chatHistory.SelectionColor = color;
            chatHistory.SelectionFont = new Font(chatHistory.Font, FontStyle.Bold);

            // Add message content
            chatHistory.AppendText(message + "\n\n");
            
            // Scroll to end
            chatHistory.SelectionStart = chatHistory.TextLength;
            chatHistory.ScrollToCaret();
        }

        private void RemoveLastLine()
        {
            if (chatHistory.InvokeRequired)
            {
                chatHistory.Invoke(new Action(() => RemoveLastLine()));
                return;
            }

            int lastNewline = chatHistory.Text.LastIndexOf(Environment.NewLine);
            if (lastNewline > 0)
            {
                chatHistory.Text = chatHistory.Text.Substring(0, lastNewline);
            }
        }

        private void ApplyAssistantAsEdit()
        {
            if (string.IsNullOrEmpty(lastAssistantText))
            {
                MessageBox.Show("No assistant response to apply.", "Nothing to Apply", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ApplyCodeAutomatically();
        }

        private void ApplyCodeAutomatically()
        {
            try
            {
                Npp.editor.BeginUndoAction();

                string codeToApply = lastAssistantText.Trim();
                
                int selStart = (int)Npp.editor.GetSelectionStart();
                int selEnd = (int)Npp.editor.GetSelectionEnd();
                bool hasSelection = selStart != selEnd;

                if (hasSelection)
                {
                    // Replace selected text
                    Npp.editor.ReplaceSel(codeToApply);
                    AppendToHistoryFormatted("System", "✓ Replaced selected text", SystemColors.GrayText);
                }
                else
                {
                    // No selection - replace entire file
                    Npp.editor.SetText(codeToApply);
                    AppendToHistoryFormatted("System", "✓ Replaced file content", SystemColors.GrayText);
                }

                Npp.editor.EndUndoAction();
            }
            catch (Exception ex)
            {
                try { Npp.editor.EndUndoAction(); } catch { }
                AppendToHistoryFormatted("System", $"✗ Error: {ex.Message}", Color.Red);
            }
        }

        private void ClearConversationHistory()
        {
            conversationHistory.Clear();
            chatHistory.Clear();
            AppendToHistoryFormatted("System", "Conversation history cleared.", SystemColors.GrayText);
        }
    }
}