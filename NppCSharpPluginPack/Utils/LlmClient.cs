using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NppDemo.Utils
{
    public class ChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    public static class LlmClient
    {
        private static readonly HttpClient http;

        static LlmClient()
        {
            // Allow all SSL certificates for GitHub's endpoint
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;
            
            // Enable TLS 1.2 and higher
            System.Net.ServicePointManager.SecurityProtocol = 
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            http = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(180)
            };
        }

        private static readonly string SystemPrompt = @"You are an AI assistant integrated into a Notepad++ plugin. You help users edit and manipulate text/code in their editor.

When providing code or text that should be inserted into the editor, use this format:
[CODE_BLOCK auto]
<language>
<your code/text here>
[/CODE_BLOCK]

Use [CODE_BLOCK auto] if the code should automatically replace the file or selection (e.g., complete file rewrites).
Use [CODE_BLOCK] if the code is just a snippet and should be manually applied.

Everything outside [CODE_BLOCK] tags is treated as explanation or conversation. Everything inside is treated as content to be pasted into the editor.

Current editor file information will be provided when available.";

        public static async Task<string> SendPromptAsync(string endpoint, string token, string model, string userMessage, List<ChatMessage> conversationHistory, string editorFileContent = null, bool includeEditorContext = true)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("endpoint empty", nameof(endpoint));
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("token empty", nameof(token));
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("model empty", nameof(model));

            // Build messages array with system prompt and conversation history
            var messages = new List<ChatMessage>
            {
                new ChatMessage("system", SystemPrompt)
            };

            // Add conversation history
            if (conversationHistory != null)
            {
                messages.AddRange(conversationHistory);
            }

            // Add current user message with optional editor context
            string messageContent = userMessage;
            if (includeEditorContext && !string.IsNullOrEmpty(editorFileContent))
            {
                messageContent = $"{userMessage}\n\n[Current editor content]:\n```\n{editorFileContent}\n```";
            }
            messages.Add(new ChatMessage("user", messageContent));

            var payload = new
            {
                model,
                messages
            };

            string content = JsonConvert.SerializeObject(payload);

            using (var req = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                req.Content = new StringContent(content, Encoding.UTF8, "application/json");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using (var resp = await http.SendAsync(req).ConfigureAwait(false))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"HTTP {(int)resp.StatusCode}: {responseBody}");
                    }

                    try
                    {
                        var root = JObject.Parse(responseBody);
                        var choices = root["choices"];
                        if (choices != null && choices.Type == JTokenType.Array && choices.HasValues)
                        {
                            var first = choices.First;
                            // OpenAI-style response
                            if (first["message"]?["content"] != null)
                                return (string)first["message"]["content"];
                            // Fallback for text field
                            if (first["text"] != null)
                                return (string)first["text"];
                        }
                    }
                    catch { /* fall through */ }

                    return responseBody;
                }
            }
        }

        public static (string explanation, string codeBlock, bool autoApply) ExtractCodeBlock(string response)
        {
            const string codeStart = "[CODE_BLOCK";
            const string codeEnd = "[/CODE_BLOCK]";

            int startIdx = response.IndexOf(codeStart);
            int endIdx = response.IndexOf(codeEnd);

            if (startIdx == -1 || endIdx == -1 || startIdx >= endIdx)
            {
                // No code block found, return entire response as explanation
                return (response, null, false);
            }

            // Check if auto-apply is enabled
            int closeIdx = response.IndexOf(']', startIdx);
            string blockHeader = response.Substring(startIdx, closeIdx - startIdx + 1);
            bool autoApply = blockHeader.Contains("auto");

            // Extract text before code block and trim
            string explanation = response.Substring(0, startIdx).Trim();

            // Extract content between markers
            int codeStartPos = closeIdx + 1;
            int codeLength = endIdx - codeStartPos;
            string rawCodeContent = response.Substring(codeStartPos, codeLength);
            
            // Clean up the code block
            string[] lines = rawCodeContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            List<string> codeLines = new List<string>();
            int startLine = 0;
            
            // Skip the first line if it's just whitespace
            if (lines.Length > 0 && string.IsNullOrWhiteSpace(lines[0]))
            {
                startLine = 1;
            }
            
            // Add remaining lines
            for (int i = startLine; i < lines.Length; i++)
            {
                codeLines.Add(lines[i]);
            }
            
            // Trim trailing empty lines
            while (codeLines.Count > 0 && string.IsNullOrWhiteSpace(codeLines[codeLines.Count - 1]))
            {
                codeLines.RemoveAt(codeLines.Count - 1);
            }
            
            string codeBlock = string.Join(Environment.NewLine, codeLines).Trim();

            // Extract text after code block
            string postExplanation = response.Substring(endIdx + codeEnd.Length).Trim();
            if (!string.IsNullOrEmpty(postExplanation))
            {
                explanation = (explanation + " " + postExplanation).Trim();
            }

            return (explanation, codeBlock, autoApply);
        }
    }
}