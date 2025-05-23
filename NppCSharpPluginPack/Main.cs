// NPP plugin platform for .Net v0.91.57 by Kasper B. Graversen etc.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Kbg.NppPluginNET.PluginInfrastructure;
using NppDemo.Utils;
using NppDemo.Forms;
using System.Linq;
using System.IO;
using System.Reflection;
using NppDemo.JSON_Tools;

namespace Kbg.NppPluginNET
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "NppAiChat";
        public static readonly string PluginConfigDirectory = Path.Combine(Npp.notepad.GetConfigDirectory(), PluginName);
        static Icon dockingFormIcon = null;

        public static Settings settings = new Settings();
        // forms
        static internal int IdAboutForm = -1;
        static internal int IdChatForm = -1;

        static NppDemo.Forms.ChatDockForm chatDockForm = null;

        // restored items that other parts of the project expect
        public static string PluginRepository = ""; // used by Docs/About forms
        public static bool isShuttingDown = false;
        public static int IdCloseHtmlTag = -1;
        #endregion

        #region " Startup/CleanUp "

        static internal void CommandMenuInit()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LoadDependency;
            Translator.ResetTranslations(true);

            // minimal menu: Docs, About, Settings, Open Assistant Chat
            PluginBase.SetCommand(0, Translator.GetTranslatedMenuItem("&Documentation"), Docs);
            PluginBase.SetCommand(1, Translator.GetTranslatedMenuItem("A&bout"), ShowAboutForm); IdAboutForm = 1;
            PluginBase.SetCommand(2, Translator.GetTranslatedMenuItem("&Settings"), OpenSettings);
            PluginBase.SetCommand(3, Translator.GetTranslatedMenuItem("Open Assistant Chat"), OpenChatDockForm); IdChatForm = 3;
        }

        private static Assembly LoadDependency(object sender, ResolveEventArgs args)
        {
            string assemblyFile = Path.Combine(Npp.pluginDllDirectory, new AssemblyName(args.Name).Name) + ".dll";
            if (File.Exists(assemblyFile))
                return Assembly.LoadFrom(assemblyFile);
            return null;
        }

        static internal void PluginCleanUp()
        {
            isShuttingDown = true;
            if (chatDockForm != null && !chatDockForm.IsDisposed)
            {
                chatDockForm.Close();
                chatDockForm.Dispose();
            }
        }
        #endregion

        #region " Menu functions "
        private static void Docs()
        {
            OpenUrlInWebBrowser(PluginRepository);
        }

        public static void OpenUrlInWebBrowser(string url)
        {
            try
            {
                var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                Translator.ShowTranslatedMessageBox("While attempting to open URL {0} in web browser, got exception\r\n{1}",
                    "Could not open url in web browser",
                    MessageBoxButtons.OK, MessageBoxIcon.Error,
                    2, url, ex);
            }
        }

        static void OpenSettings()
        {
            settings.ShowDialog();
        }

        static void ShowAboutForm()
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
            aboutForm.Focus();
        }

        public static void OpenChatDockForm()
        {
            bool wasVisible = chatDockForm != null && chatDockForm.Visible;
            if (wasVisible)
                Npp.notepad.HideDockingForm(chatDockForm);
            else if (chatDockForm == null || chatDockForm.IsDisposed)
            {
                chatDockForm = new NppDemo.Forms.ChatDockForm();
                DisplayChatDockForm(chatDockForm);
            }
            else
            {
                Npp.notepad.ShowDockingForm(chatDockForm);
            }
        }

        private static void DisplayChatDockForm(NppDemo.Forms.ChatDockForm form)
        {
            using (Bitmap newBmp = new Bitmap(16, 16))
            {
                Graphics g = Graphics.FromImage(newBmp);
                ColorMap[] colorMap = new ColorMap[1];
                colorMap[0] = new ColorMap();
                colorMap[0].OldColor = Color.Fuchsia;
                colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                ImageAttributes attr = new ImageAttributes();
                attr.SetRemapTable(colorMap);
                dockingFormIcon = Icon.FromHandle(newBmp.GetHicon());
            }

            NppTbData _nppTbData = new NppTbData();
            _nppTbData.hClient = form.Handle;
            _nppTbData.pszName = "Assistant";
            _nppTbData.dlgID = IdChatForm;
            _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_LEFT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
            _nppTbData.hIconTab = (uint)dockingFormIcon.Handle;
            _nppTbData.pszModuleName = PluginName;
            IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
            Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            Npp.notepad.ShowDockingForm(form);
        }
        #endregion
    }
}
