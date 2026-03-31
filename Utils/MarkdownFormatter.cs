using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NppAiChat.Utils;

/// <summary>
/// Handles markdown formatting for RichTextBox controls
/// </summary>
public static class MarkdownFormatter
{
    /// <summary>
    /// Appends formatted markdown text to a RichTextBox
    /// </summary>
    public static void AppendFormattedMarkdown(RichTextBox textBox, string text)
    {
        try
        {
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            bool inCodeBlock = false;
            List<string> tableRows = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                // Handle table collection
                if (tableRows != null)
                {
                    // Check if this is still a table row
                    if (trimmed.StartsWith("|") && trimmed.EndsWith("|"))
                    {
                        // Skip separator lines
                        if (!IsTableSeparator(trimmed))
                        {
                            tableRows.Add(trimmed);
                        }
                        continue;
                    }
                    else
                    {
                        // Table ended, render it
                        RenderTable(textBox, tableRows);
                        tableRows = null;
                    }
                }

                // Detect table start
                if (trimmed.StartsWith("|") && trimmed.EndsWith("|") && !IsTableSeparator(trimmed))
                {
                    tableRows = new List<string> { trimmed };
                    continue;
                }

                // Code blocks
                if (trimmed.StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    AppendText(textBox, Environment.NewLine);
                    continue;
                }

                if (inCodeBlock)
                {
                    int start = textBox.TextLength;
                    AppendText(textBox, line + Environment.NewLine);
                    textBox.Select(start, line.Length);
                    textBox.SelectionFont = new Font("Consolas", textBox.Font.Size - 1);
                    textBox.SelectionColor = Color.FromArgb(100, 100, 100);
                    continue;
                }

                // Headers
                if (trimmed.StartsWith("### "))
                {
                    int start = textBox.TextLength;
                    AppendText(textBox, trimmed.Substring(4) + Environment.NewLine);
                    textBox.Select(start, trimmed.Length - 4);
                    textBox.SelectionFont = new Font(textBox.Font, FontStyle.Bold);
                    textBox.SelectionColor = Color.FromArgb(0, 100, 150);
                    continue;
                }
                if (trimmed.StartsWith("## "))
                {
                    int start = textBox.TextLength;
                    AppendText(textBox, trimmed.Substring(3) + Environment.NewLine);
                    textBox.Select(start, trimmed.Length - 3);
                    textBox.SelectionFont = new Font(textBox.Font, FontStyle.Bold);
                    textBox.SelectionColor = Color.FromArgb(0, 80, 120);
                    continue;
                }
                if (trimmed.StartsWith("# "))
                {
                    int start = textBox.TextLength;
                    AppendText(textBox, trimmed.Substring(2) + Environment.NewLine);
                    textBox.Select(start, trimmed.Length - 2);
                    textBox.SelectionFont = new Font(textBox.Font, FontStyle.Bold);
                    textBox.SelectionColor = Color.FromArgb(0, 60, 100);
                    continue;
                }

                // Lists
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    AppendText(textBox, "  • " + trimmed.Substring(2) + Environment.NewLine);
                    continue;
                }

                // Horizontal rules
                if (trimmed == "---" || trimmed == "***" || trimmed == "___")
                {
                    AppendText(textBox, "----------------------------------------" + Environment.NewLine);
                    continue;
                }

                // Regular text - just strip markdown syntax for cleaner display
                string cleaned = line
                    .Replace("**", "")
                    .Replace("__", "")
                    .Replace("`", "")
                    .Replace("*", "")
                    .Replace("_", "");

                AppendText(textBox, cleaned + Environment.NewLine);
            }

            // Handle table at end of text
            if (tableRows != null)
            {
                RenderTable(textBox, tableRows);
            }

            AppendText(textBox, Environment.NewLine);
        }
        catch (Exception ex)
        {
            // If anything fails, log the error and append the original text
            AppendText(textBox, "[Formatting error: " + ex.Message + "]" + Environment.NewLine);
            AppendText(textBox, text + Environment.NewLine + Environment.NewLine);
        }
    }

    private static bool IsTableSeparator(string line)
    {
        // Check if line is a table separator like |---|---|
        string trimmed = line.Trim('|').Trim();
        if (string.IsNullOrEmpty(trimmed)) return false;

        foreach (char c in trimmed)
        {
            if (c != '-' && c != ' ' && c != ':')
                return false;
        }
        return true;
    }

    private static void RenderTable(RichTextBox textBox, List<string> rows)
    {
        if (rows.Count == 0) return;

        // Parse all rows into cells
        var tableData = new List<string[]>();
        var columnWidths = new List<int>();

        foreach (string row in rows)
        {
            string[] cells = row.Trim('|').Split('|');
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = cells[i].Trim();
            }
            tableData.Add(cells);

            // Track max width for each column
            for (int i = 0; i < cells.Length; i++)
            {
                int width = cells[i].Length;
                if (columnWidths.Count <= i)
                    columnWidths.Add(width);
                else
                    columnWidths[i] = Math.Max(columnWidths[i], width);
            }
        }

        // Render table with monospace font for alignment
        bool isHeader = true;
        foreach (string[] row in tableData)
        {
            int start = textBox.TextLength;
            string formattedRow = FormatTableRow(row, columnWidths);
            AppendText(textBox, formattedRow + Environment.NewLine);

            // Bold the header row
            if (isHeader)
            {
                textBox.Select(start, formattedRow.Length);
                textBox.SelectionFont = new Font("Consolas", textBox.Font.Size - 1, FontStyle.Bold);
                isHeader = false;
            }
            else
            {
                textBox.Select(start, formattedRow.Length);
                textBox.SelectionFont = new Font("Consolas", textBox.Font.Size - 1);
            }
        }
    }

    private static string FormatTableRow(string[] cells, List<int> columnWidths)
    {
        var parts = new List<string>();
        for (int i = 0; i < cells.Length; i++)
        {
            int width = i < columnWidths.Count ? columnWidths[i] : cells[i].Length;
            parts.Add(cells[i].PadRight(width));
        }
        return "| " + string.Join(" | ", parts) + " |";
    }

    private static void AppendText(RichTextBox textBox, string text)
    {
        if (textBox.InvokeRequired)
        {
            textBox.Invoke(new Action(() => textBox.AppendText(text)));
        }
        else
        {
            textBox.AppendText(text);
        }
    }
}
