using System.Drawing;
using System.Windows.Forms;

namespace VersionInfo;

public sealed class Version
{
    private const string ReleaseNotesPlaceholder = nameof(ReleaseNotesPlaceholder);

    public static void Display()
    {
        ShowReleaseNotesDialog();
    }

    private static void ShowReleaseNotesDialog()
    {
        var releaseNotes = GetReleaseNotes();
        if (releaseNotes == ReleaseNotesPlaceholder)
        {
            return;
        }

        var form = new Form
        {
            Text = "Release Notes - Version 2.5.0",
            Size = new Size(800, 600),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var titleLabel = new Label
        {
            Text = "What's New in Version 2.5.0",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true
        };

        var notesTextBox = new RichTextBox
        {
            Text = releaseNotes,
            Location = new Point(20, 60),
            Size = new Size(740, 450),
            ReadOnly = true,
            BackColor = SystemColors.Window,
            Font = new Font("Consolas", 10),
            ScrollBars = RichTextBoxScrollBars.Vertical,
            DetectUrls = true,
            BorderStyle = BorderStyle.FixedSingle
        };

        var okButton = new Button
        {
            Text = "OK",
            Location = new Point(350, 530),
            Size = new Size(100, 30),
            Font = new Font("Segoe UI", 10)
        };

        okButton.Click += (sender, e) => form.Close();

        form.Controls.Add(titleLabel);
        form.Controls.Add(notesTextBox);
        form.Controls.Add(okButton);

        form.FormClosed += (sender, e) => form.Dispose();
        form.ShowDialog();
    }

    private static string GetReleaseNotes()
    {
        return ReleaseNotesPlaceholder;
    }

}
