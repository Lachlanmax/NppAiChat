using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NppAiChat.Utils;
using NppAiChat.PluginInfrastructure;

namespace NppAiChat.Forms;

public class ChatDockForm : FormBase
{
    // UI Constants
    private const int ButtonHeight = 30;
    private const int ButtonWidth = 70;
    private const int InputBoxHeight = 160;
    private const int StatusLabelHeight = 25;
    private const int ButtonPanelHeight = 40;
    private const int FileInfoSearchWindow = 500;

    private RichTextBox chatHistory;
    private TextBox inputBox;
    private Button sendBtn;
    private Button stopBtn;
    private Button revertBtn;
    private Label statusLabel;
    private System.Windows.Forms.Timer animationTimer;
    private System.Windows.Forms.Timer focusTimer;
    private int animationStep = 0;
    private string baseStatusText = "";
    private List<ChatMessage> conversationHistory = new List<ChatMessage>();
    private bool isSending = false;
    private CancellationTokenSource cancellationTokenSource;
    private bool IsLoaded = false;

    public ChatDockForm() : base(false, true)
    {
        Text = "Assistant";
        ClientSize = new Size(500, 800);

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = ButtonPanelHeight + 8,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8, 4, 8, 4)
        };

        sendBtn = new Button
        {
            Text = "Send",
            Width = ButtonWidth,
            Height = ButtonHeight,
            FlatStyle = FlatStyle.System,
            Font = new Font("Segoe UI", 8, FontStyle.Bold)
        };
        stopBtn = new Button
        {
            Text = "Stop",
            Width = ButtonWidth,
            Height = ButtonHeight,
            FlatStyle = FlatStyle.System,
            Font = new Font("Segoe UI", 8),
            Enabled = false
        };
        revertBtn = new Button
        {
            Text = "Revert",
            Width = ButtonWidth,
            Height = ButtonHeight,
            FlatStyle = FlatStyle.System,
            Font = new Font("Segoe UI", 8)
        };
        var clearBtn = new Button
        {
            Text = "Clear Context",
            Width = 110,
            Height = ButtonHeight,
            FlatStyle = FlatStyle.System,
            Font = new Font("Segoe UI", 8)
        };

        panel.Controls.Add(sendBtn);
        panel.Controls.Add(stopBtn);
        panel.Controls.Add(revertBtn);
        panel.Controls.Add(clearBtn);

        var statusContainer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = StatusLabelHeight + 4,
            Padding = new Padding(8, 2, 8, 2)
        };

        statusLabel = new Label
        {
            Text = "",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        statusContainer.Controls.Add(statusLabel);

        var inputContainer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = InputBoxHeight + 8,
            Padding = new Padding(8, 4, 8, 4)
        };

        inputBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.Fixed3D,
            AcceptsTab = false,
            WordWrap = true
        };
        inputContainer.Controls.Add(inputBox);

        chatHistory = new RichTextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8)
        };

        Controls.Add(chatHistory);
        Controls.Add(panel);
        Controls.Add(statusContainer);
        Controls.Add(inputContainer);

        sendBtn.Click += async (s, e) => await SendPromptAsync();
        stopBtn.Click += (s, e) => StopStreaming();
        revertBtn.Click += (s, e) => RevertChanges();
        clearBtn.Click += (s, e) => ClearConversationHistory();
        chatHistory.KeyDown += (s, e) => HandleGlobalKeyDown(e);
        KeyDown += (s, e) => HandleGlobalKeyDown(e);

        animationTimer = new System.Windows.Forms.Timer { Interval = 500 };
        animationTimer.Tick += AnimationTimer_Tick;

        focusTimer = new System.Windows.Forms.Timer { Interval = 100 };
        focusTimer.Tick += (s, e) =>
        {
            focusTimer.Stop();
            inputBox.Focus();
        };
        VisibleChanged += (s, e) =>
        {
            if (!IsLoaded)
            {
                IsLoaded = true;
                inputBox.KeyUp += InputBox_KeyUp;
                inputBox.KeyDown += InputBox_KeyDown;
            }
            if (Visible)
            {
                focusTimer.Start();
                // Apply theme colors when the form becomes visible
                BeginInvoke(new Action(() => UpdateThemeColors()));
            }
        };

        FormClosing += (s, e) =>
        {
            Main.isChatFormVisible = false;
        };

        Translator.TranslateForm(this);
    }

    public void UpdateThemeColors()
    {
        // Get the editor background color to determine if we're in dark mode
        Color editorBg = Npp.notepad.GetDefaultBackgroundColor();
        bool isDarkMode = !(editorBg.R > 240 && editorBg.G > 240 && editorBg.B > 240);

        // Update status label color to be visible in both light and dark modes
        if (isDarkMode)
        {
            statusLabel.ForeColor = Color.FromArgb(255, 220, 100); // Light orange/yellow for dark mode
        }
        else
        {
            statusLabel.ForeColor = Color.FromArgb(255, 140, 0); // Orange for light mode
        }
    }

    private Color GetUserMessageColor()
    {
        Color editorBg = Npp.notepad.GetDefaultBackgroundColor();
        bool isDarkMode = !(editorBg.R > 240 && editorBg.G > 240 && editorBg.B > 240);
        return isDarkMode ? Color.FromArgb(100, 200, 100) : Color.FromArgb(0, 100, 0);
    }

    private Color GetAssistantMessageColor()
    {
        Color editorBg = Npp.notepad.GetDefaultBackgroundColor();
        bool isDarkMode = !(editorBg.R > 240 && editorBg.G > 240 && editorBg.B > 240);
        return isDarkMode ? Color.FromArgb(100, 150, 255) : Color.FromArgb(0, 0, 180);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            animationTimer?.Dispose();
            focusTimer?.Dispose();
            cancellationTokenSource?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InputBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift && !e.Control && !e.Alt)
        {
            _ = SendPromptAsync();
        }
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Handle Ctrl+Shift+A to toggle the chat form
        if (e.Control && e.Shift && e.KeyCode == Keys.A)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            Main.ToggleChatForm();
        }
    }

    private void HandleGlobalKeyDown(KeyEventArgs e)
    {
        // Handle Ctrl+Shift+A to toggle the chat form
        if (e.Control && e.Shift && e.KeyCode == Keys.A)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            Main.ToggleChatForm();
        }
    }

    private static string GetCurrentFileContent(int maxChars = 30_000)
    {
        if (Npp.editor == null)
        {
            return string.Empty;
        }

        var length = Npp.editor.GetLength();
        var charsToRead = Math.Min(length, maxChars);

        var textRange = new TextRange(0, (int)charsToRead, (int)charsToRead + 1);
        Npp.editor.GetTextRange(textRange);
        string txt = textRange.lpstrText ?? string.Empty;
        if (length > maxChars)
        {
            txt += Environment.NewLine + "...[truncated]";
        }

        return txt;
    }

    private async Task SendPromptAsync()
    {
        if (isSending) return;

        var prompt = inputBox.Text.Trim();
        if (string.IsNullOrEmpty(prompt)) return;

        isSending = true;
        sendBtn.Enabled = false;
        sendBtn.Text = "...";
        stopBtn.Enabled = true;
        cancellationTokenSource = new CancellationTokenSource();

        UpdateStatus("Prompt sent! Waiting for response...");

        Color userColor = GetUserMessageColor();
        AppendToHistoryFormatted("User", prompt, userColor);
        inputBox.Clear();

        string endpoint = Main.settings.llm_endpoint;
        string token = Main.settings.llm_token;
        string model = Main.settings.llm_model;

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(token))
        {
            AppendToHistoryFormatted("Assistant", "Error - LLM endpoint or token not configured. Please check settings.", Color.Red);
            ResetSendButton();
            return;
        }

        Color assistantColor = GetAssistantMessageColor();
        AppendToHistoryFormatted("Assistant", "", assistantColor);

        UpdateStatus("Processing");

        try
        {
            // Always include editor context - this is the core functionality
            string editorContent = GetCurrentFileContent();

            string fullResponse = await LlmClient.SendPromptAsync(
                endpoint,
                token,
                model,
                prompt,
                conversationHistory,
                editorContent,
                true);

            UpdateStatus("Response received!");

            ParseAndCreateFiles(fullResponse);

            conversationHistory.Add(new ChatMessage("user", prompt));
            conversationHistory.Add(new ChatMessage("assistant", fullResponse));
        }
        catch (OperationCanceledException)
        {
            AppendToChatHistory(Environment.NewLine + "[Request stopped by user]" + Environment.NewLine + Environment.NewLine);
            UpdateStatus("Request stopped");
        }
        catch (Exception ex)
        {
            AppendToChatHistory(Environment.NewLine + "(error) " + ex.Message + Environment.NewLine + Environment.NewLine);
            UpdateStatus("Error: " + ex.Message);
        }
        finally
        {
            ResetSendButton();
        }
    }

    private void ParseAndCreateFiles(string response)
    {
        try
        {
            var files = new List<(string fileName, string content, string language)>();
            int pos = 0;

            while (true)
            {
                int codeStart = response.IndexOf("[CODE_BLOCK", pos);
                if (codeStart == -1) break;

                int codeStartEnd = response.IndexOf("]", codeStart);
                if (codeStartEnd == -1) break;

                int codeEnd = response.IndexOf("[/CODE_BLOCK]", codeStartEnd);
                if (codeEnd == -1) break;

                string codeContent = response.Substring(codeStartEnd + 1, codeEnd - codeStartEnd - 1).Trim();

                if (!string.IsNullOrEmpty(codeContent))
                {
                    string fileName = ExtractFileNameFromBlock(response, codeStart);
                    string language = ExtractLanguageFromBlock(response, codeStart, codeStartEnd);
                    files.Add((fileName, codeContent, language));
                }

                pos = codeEnd + "[/CODE_BLOCK]".Length;
            }

            if (files.Count > 0)
            {
                AppendToChatHistory(Environment.NewLine + "[Processing " + files.Count + " file(s)...]" + Environment.NewLine);

                for (int i = 0; i < files.Count; i++)
                {
                    var (fileName, content, language) = files[i];
                    CreateFile(fileName, content, language);
                }
            }
            else
            {
                MarkdownFormatter.AppendFormattedMarkdown(chatHistory, response);
            }
        }
        catch (Exception ex)
        {
            AppendToChatHistory(Environment.NewLine + "[Error parsing files: " + ex.Message + "]" + Environment.NewLine);
        }
    }

    private string ExtractFileNameFromBlock(string response, int codeBlockStart)
    {
        try
        {
            int searchStart = Math.Max(0, codeBlockStart - FileInfoSearchWindow);
            int searchEnd = codeBlockStart;
            string precedingText = response.Substring(searchStart, searchEnd - searchStart);

            int fileInfoPos = precedingText.LastIndexOf("<file_info>");
            if (fileInfoPos != -1)
            {
                int nameStart = precedingText.IndexOf("<name>", fileInfoPos);
                int nameEnd = precedingText.IndexOf("</name>", nameStart);
                if (nameStart != -1 && nameEnd != -1)
                {
                    return precedingText.Substring(nameStart + 6, nameEnd - nameStart - 6).Trim();
                }
            }

            return "";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in {nameof(ExtractFileNameFromBlock)}: {ex.Message}");
            return "";
        }
    }

    private string ExtractLanguageFromBlock(string response, int codeBlockStart, int codeStartEnd)
    {
        try
        {
            string blockContent = response.Substring(codeBlockStart, codeStartEnd - codeBlockStart + 1);
            int langStart = blockContent.IndexOf("language=\"");
            if (langStart != -1)
            {
                langStart += "language=\"".Length;
                int langEnd = blockContent.IndexOf("\"", langStart);
                if (langEnd != -1)
                {
                    return blockContent.Substring(langStart, langEnd - langStart).ToLower();
                }
            }
            return "";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in {nameof(ExtractLanguageFromBlock)}: {ex.Message}");
            return "";
        }
    }

    private string GetFileExtensionFromLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
            return ".txt";

        switch (language.ToLower())
        {
            case "csharp":
            case "c#":
                return ".cs";
            case "javascript":
            case "js":
                return ".js";
            case "typescript":
            case "ts":
                return ".ts";
            case "python":
            case "py":
                return ".py";
            case "java":
                return ".java";
            case "cpp":
            case "c++":
                return ".cpp";
            case "c":
                return ".c";
            case "html":
                return ".html";
            case "css":
                return ".css";
            case "xml":
                return ".xml";
            case "json":
                return ".json";
            case "sql":
                return ".sql";
            case "bash":
            case "sh":
                return ".sh";
            case "powershell":
            case "ps1":
                return ".ps1";
            case "php":
                return ".php";
            case "ruby":
            case "rb":
                return ".rb";
            case "go":
                return ".go";
            case "rust":
            case "rs":
                return ".rs";
            case "swift":
                return ".swift";
            case "kotlin":
            case "kt":
                return ".kt";
            case "dart":
                return ".dart";
            default:
                return ".txt";
        }
    }

    private void CreateFile(string fileName, string content, string language = "")
    {
        try
        {
            string currentFilePath = Npp.notepad.GetCurrentFilePath();

            string tempDir = System.IO.Path.GetTempPath();
            string filePath = null;

            if (string.IsNullOrEmpty(fileName))
            {
                if (!string.IsNullOrEmpty(currentFilePath))
                {
                    Npp.editor.SetText(content);
                    AppendToChatHistory("  - Updated current file" + Environment.NewLine + Environment.NewLine);
                }
                else
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string extension = GetFileExtensionFromLanguage(language);
                    string defaultFileName = "generated_" + timestamp + extension;
                    filePath = System.IO.Path.Combine(tempDir, defaultFileName);
                    System.IO.File.WriteAllText(filePath, content);
                    Npp.notepad.OpenFile(filePath);
                    AppendToChatHistory("  - Created: " + defaultFileName + Environment.NewLine + Environment.NewLine);
                }
                return;
            }

            if (!string.IsNullOrEmpty(currentFilePath))
            {
                string currentFileName = System.IO.Path.GetFileName(currentFilePath);
                if (currentFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    Npp.editor.SetText(content);
                    AppendToChatHistory("  - Updated: " + fileName + Environment.NewLine);
                    return;
                }
            }

            filePath = System.IO.Path.Combine(tempDir, fileName);

            System.IO.File.WriteAllText(filePath, content);

            Npp.notepad.OpenFile(filePath);

            AppendToChatHistory("  - Created: " + fileName + Environment.NewLine);
        }
        catch (Exception ex)
        {
            AppendToChatHistory("  - Failed to create " + fileName + ": " + ex.Message + Environment.NewLine);
        }
    }

    private void StopStreaming()
    {
        if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
        {
            cancellationTokenSource.Cancel();
        }
    }

    private void RevertChanges()
    {
        try
        {
            Npp.editor.Undo();
            AppendToHistoryFormatted("System", "✓ Changes reverted", SystemColors.GrayText);
        }
        catch (Exception ex)
        {
            AppendToHistoryFormatted("System", "✗ Error reverting: " + ex.Message, Color.Red);
        }
    }

    private void ResetSendButton()
    {
        isSending = false;
        if (chatHistory.InvokeRequired)
        {
            chatHistory.Invoke(new Action(() =>
            {
                sendBtn.Enabled = true;
                sendBtn.Text = "Send";
                stopBtn.Enabled = false;
                UpdateStatus("");
            }));
        }
        else
        {
            sendBtn.Enabled = true;
            sendBtn.Text = "Send";
            stopBtn.Enabled = false;
            UpdateStatus("");
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusLabel.InvokeRequired)
        {
            statusLabel.Invoke(new Action(() => UpdateStatus(message)));
            return;
        }

        baseStatusText = message;
        animationStep = 0;

        if (message.Contains("Processing"))
        {
            animationTimer.Start();
            UpdateAnimatedStatus();
        }
        else
        {
            animationTimer.Stop();
            statusLabel.Text = message;
        }
    }

    private void AnimationTimer_Tick(object sender, EventArgs e)
    {
        animationStep = (animationStep + 1) % 4;
        UpdateAnimatedStatus();
    }

    private void UpdateAnimatedStatus()
    {
        if (statusLabel.InvokeRequired)
        {
            statusLabel.Invoke(new Action(UpdateAnimatedStatus));
            return;
        }

        string dots = new string('.', animationStep);
        statusLabel.Text = baseStatusText + dots;
    }

    private void AppendToChatHistory(string text)
    {
        if (chatHistory.InvokeRequired)
        {
            chatHistory.Invoke(new Action(() => AppendToChatHistory(text)));
            return;
        }

        chatHistory.AppendText(text);
        chatHistory.SelectionStart = chatHistory.TextLength;
        chatHistory.ScrollToCaret();
    }

    private void AppendToHistoryFormatted(string speaker, string message, Color color)
    {
        if (chatHistory.InvokeRequired)
        {
            chatHistory.Invoke(new Action(() => AppendToHistoryFormatted(speaker, message, color)));
            return;
        }

        chatHistory.AppendText(speaker + ": ");
        int startIndex = chatHistory.TextLength - (speaker.Length + 2);
        chatHistory.Select(startIndex, speaker.Length + 1);
        chatHistory.SelectionColor = color;
        chatHistory.SelectionFont = new Font(chatHistory.Font, FontStyle.Bold);

        chatHistory.AppendText(message + Environment.NewLine + Environment.NewLine);

        chatHistory.SelectionStart = chatHistory.TextLength;
        chatHistory.ScrollToCaret();
    }

    private void ClearConversationHistory()
    {
        conversationHistory.Clear();
        chatHistory.Clear();
        AppendToHistoryFormatted("System", "Conversation history cleared.", SystemColors.GrayText);
    }
}
