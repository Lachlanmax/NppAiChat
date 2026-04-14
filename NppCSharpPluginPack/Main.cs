using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NppAiChat.Forms;
using NppAiChat.PluginInfrastructure;
using NppAiChat.Utils;

namespace NppAiChat;

class Main
{
    #region Fields
    internal const string PluginName = "NppAiChat";
    public static readonly string PluginConfigDirectory = Path.Combine(Npp.notepad.GetConfigDirectory(), PluginName);
    static Icon dockingFormIcon = null;

    public static Settings settings = null;
    internal static int IdChatForm = -1;

    internal static ChatDockForm chatDockForm = null;
    public static bool isShuttingDown = false;
    public static bool isChatFormVisible = false;
    #endregion

    #region Startup/CleanUp

    static Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += LoadDependency;

        settings = new();
    }

    internal static void CommandMenuInit()
    {
        Translator.ResetTranslations(true);

        PluginBase.SetCommand(0, Translator.GetTranslatedMenuItem("&Settings"), OpenSettings);
        PluginBase.SetCommand(1, Translator.GetTranslatedMenuItem("Open Assistant Chat\tCtrl+Shift+A"), OpenChatDockForm, new ShortcutKey(true, false, true, Keys.A)); IdChatForm = 1;
    }

    private static Assembly LoadDependency(object sender, ResolveEventArgs args)
    {
        return LoadFromFile(args) ?? LoadFromResource(args);
    }

    private static Assembly LoadFromFile(ResolveEventArgs args)
    {
        try
        {
            var assemblyName = new AssemblyName(args.Name).Name;
            var assemblyFile = Path.Combine(Npp.pluginDllDirectory, assemblyName + ".dll");
            return File.Exists(assemblyFile) ? Assembly.LoadFrom(assemblyFile) : null;
        }
        catch
        {
            return null;
        }
    }

    private static Assembly LoadFromResource(ResolveEventArgs args)
    {
        try
        {
            var assemblyName = new AssemblyName(args.Name).Name;
            using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(assemblyName);
            if (resourceStream == null)
                return null;

            using var compressedStream = new MemoryStream();
            resourceStream.CopyTo(compressedStream);
            compressedStream.Position = 0;

            using var zipArchive = new ZipArchive(compressedStream, ZipArchiveMode.Read);
            var entry = zipArchive.Entries.FirstOrDefault();
            if (entry == null)
                return null;

            using var entryStream = entry.Open();
            using var decompressedData = new MemoryStream();
            entryStream.CopyTo(decompressedData);
            return Assembly.Load(decompressedData.ToArray());
        }
        catch
        {
            return null;
        }
    }

    internal static void PluginCleanUp()
    {
        isShuttingDown = true;
        isChatFormVisible = false;
        if (chatDockForm != null && !chatDockForm.IsDisposed)
        {
            chatDockForm.Close();
            chatDockForm.Dispose();
        }
    }

    internal static void OnDarkModeChanged()
    {
        // Update the chat form if it's visible
        if (chatDockForm != null && !chatDockForm.IsDisposed && isChatFormVisible)
        {
            chatDockForm.Invoke(new Action(() =>
            {
                FormStyle.ApplyStyle(chatDockForm, true);
                chatDockForm.UpdateThemeColors();
            }));
        }
    }
    #endregion

    #region Menu functions

    static void OpenSettings()
    {
        settings.ShowDialog();
    }

    public static void ToggleChatForm()
    {
        OpenChatDockForm();
    }

    private static void OpenChatDockForm()
    {
        // Use manual flag to track visibility state
        if (isChatFormVisible)
        {
            if (chatDockForm != null && !chatDockForm.IsDisposed)
            {
                Npp.notepad.HideDockingForm(chatDockForm);
            }
            isChatFormVisible = false;
        }
        else
        {
            if (chatDockForm == null || chatDockForm.IsDisposed)
            {
                chatDockForm = CreateChatDockForm();
            }

            Npp.notepad.ShowDockingForm(chatDockForm);
            isChatFormVisible = true;
        }
    }

    private static ChatDockForm CreateChatDockForm()
    {
        using (var newBmp = new Bitmap(16, 16))
        {
            var g = Graphics.FromImage(newBmp);
            var colorMap = new ColorMap[1];
            colorMap[0] = new ColorMap();
            colorMap[0].OldColor = Color.Fuchsia;
            colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
            var attr = new ImageAttributes();
            attr.SetRemapTable(colorMap);
            dockingFormIcon = Icon.FromHandle(newBmp.GetHicon());
        }

        var form = new ChatDockForm();
        var _nppTbData = new NppTbData();
        _nppTbData.hClient = form.Handle;
        _nppTbData.pszName = "Assistant";
        _nppTbData.dlgID = IdChatForm;
        _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_LEFT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
        _nppTbData.hIconTab = (uint)dockingFormIcon.Handle;
        _nppTbData.pszModuleName = PluginName;
        var _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
        Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

        Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
        return form;
    }
    #endregion
}