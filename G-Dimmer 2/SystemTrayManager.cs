using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace GDimmer
{
    public class SystemTrayManager : IDisposable
    {
        private readonly NotifyIcon notifyIcon;

        public event Action OnOpenGDimmer = delegate { };
        public event Action EnableDisableDimmer = delegate { };
        public event Action OnExit = delegate { };

        public SystemTrayManager()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = GetIconFromResources("G_Dimmer_2.Resources.system_tray.ico"),
                Visible = true,
                Text = "G-Dimmer"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add(new ToolStripMenuItem("Open G-Dimmer", GetIconFromResources("G_Dimmer_2.Resources.preferences.ico").ToBitmap(), (s, e) => OnOpenGDimmer.Invoke()));
            contextMenu.Items.Add(new ToolStripMenuItem("Enable/Disable", GetIconFromResources("G_Dimmer_2.Resources.disable.ico").ToBitmap(), (s, e) => EnableDisableDimmer.Invoke()));
            contextMenu.Items.Add(new ToolStripMenuItem("Exit", GetIconFromResources("G_Dimmer_2.Resources.exit.ico").ToBitmap(), (s, e) => OnExit.Invoke()));

            notifyIcon.ContextMenuStrip = contextMenu;
        }

        private Icon GetIconFromResources(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new Exception($"[SystemTrayManager] Icon resource '{resourceName}' not found.");
                }
                return new Icon(stream); 
            }
        }

        public void Dispose()
        {
            notifyIcon.Dispose();
        }

    }
}