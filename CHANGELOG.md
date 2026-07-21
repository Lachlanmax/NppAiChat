# Changelog

All notable changes to this project will be documented in this file.

## [1.0.3]
### Security Features
- Secure credential storage: API tokens and sensitive settings are now encrypted using DPAPI instead of being stored in plain text.
- Token field now displays `*****` instead of the actual value in the settings UI.

## [1.0.2]
### Improvements
- Support streaming text from LLM to Chat Assistant sidebar directly
- Improved Build folder structure by platform and build type

### Fixes
- Fixed some more visabilty issues

## [1.0.1]
### Improvements
- Color inheritance: Chat form now inherits colors from the main Notepad++ window for better visual integration.
- Enhanced markdown formatting: Improved markdown rendering with better color support and formatting options.
- Solution structure: Reorganized project directories for better maintainability.

### Fixes
- Fixed various build issues.
- Fixed redundant newline handling in markdown formatting.

## [1.0.0] 
### Initial Release

- AI-powered chat assistant integrated in the Notepad++ sidebar.
- Code generation, completion, and refactoring features.
- Text manipulation: formatting, summarizing, translating.
- Context-aware responses based on open file and cursor position.
- Customizable prompts and AI settings.
- Dark mode support.
- Configuration for LLM endpoint, model, and token.
- API token stored securely.
