# Notepad++ AI Chat Plugin 

An AI-powered chat assistant plugin for Notepad++ that brings the capabilities of your favorite LLM right into your favorite text editor. Enhance your workflow by asking questions, generating code, manipulating text, and receiving context-aware assistance—without leaving Notepad++.

## Features

- **Seamless sidebar integration:** Chat with an AI assistant directly within Notepad++.
- **Code generation and completion:** Generate code snippets, refactor, document functions, and more.
- **Text editing intelligence:** Request formatting, summaries, translations, and text transformations.
- **Context-aware replies:** The AI utilizes your current file contents and cursor position to provide relevant responses.
- **Customizable prompts:** Tailor AI behavior with your own instructions and commands.
- **Privacy-focused:** Your files remain local—the plugin only sends necessary data for AI processing.
- **Dark Mode Support:** No worries of weird looking sidebars




<img width="232" height="258" alt="image" src="https://github.com/user-attachments/assets/3efcac53-f2a1-49ea-9556-cb60c0da2621" />



## Installation

1. **Download** the latest release from this repository's [Releases](https://github.com/Lachlanmax/NppAiChat/releases) page (or build from source, see below).
2. **Extract** the plugin files to your Notepad++ `plugins` directory (typically `C:\Program Files\Notepad++\plugins` or `%APPDATA%\Notepad++\plugins`).
3. **Restart Notepad++.**

## Build from Source

### Prerequisites

- Visual Studio (recommended 2019 or later) with C++ toolkit
- Notepad++ plugin SDK (included or [download](https://github.com/notepad-plus-plus/nppPluginTemplate))

### Steps

1. Clone or download This repo
2. Open the solution (`.sln`) file in Visual Studio.
3. Build the solution in **Release** mode.
4. Copy the generated `.dll` to `Notepad++/plugins/NppAaChat`.

## Usage

- Open Notepad++.
- Go to **Plugins > NppAIChat** to configure your LLM details in settings
- Go to **Plugins > NppAIChat** or press **Ctrl+Shift+A** to open the Chat Assistant sidebar
- The chat assistant will appear as a sidebar
- Type your questions or commands; interact with AI as you work
- You can highlight text and request operations on selections (summarize, convert, explain, etc).

## Example Prompts

- *"Explain the selected code."*
- *"Refactor this function for performance."*
- *"Summarize the document."*
- *"Generate unit tests for this file."*

## Configuration

- **llm_endpoint:** The LLM API endpoint e.g. https://models.github.ai/inference/chat/completions
- **llm_model:** Selected model from the configured endpoint e.g. openai/gpt-5
- **llm_token:** Your personal API token

## Privacy

- File contents are only shared with the AI service when you interact with the plugin; no background uploads.
- API token is stored securely and are never uploaded plain.

## Contributing

Pull requests are welcome! Please open issues for feature suggestions or bug reports.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/YourFeature`)
3. Commit your changes
4. Push to the branch and open a Pull Request

---

## License

This project is free to use under Open Source License

## Credits

- Create by Max Lachlan
- Inspired by [nppPluginTemplate](https://github.com/notepad-plus-plus/nppPluginTemplate)
