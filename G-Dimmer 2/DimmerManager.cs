using GDimmer;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows;
using Color = System.Windows.Media.Color;

namespace G_Dimmer_2
{
    internal class DimmerManager
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020; // Allows input passthrough
        public static IntPtr overlayHandle = IntPtr.Zero;

    private Window? screenOverlay; // Store overlay reference                                      
      

        public async Task ApplyDimmingAsync()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (screenOverlay == null)
                {
                    screenOverlay = new Window
                    {
                        AllowsTransparency = true,
                        WindowStyle = WindowStyle.None,
                        Background = new SolidColorBrush(Color.FromArgb(SettingsManager.GetDimmerSliderBrightnessValue(), 0, 0, 0)), // Initial brightness
                        Topmost = true,
                        ShowInTaskbar = false,
                        Left = 0,
                        Top = 0,
                        Width = SystemParameters.PrimaryScreenWidth,
                        Height = SystemParameters.PrimaryScreenHeight
                    };
                    screenOverlay.Show();
                    IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(screenOverlay).Handle;
                    int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                    SetWindowLong(hWnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
                }
                overlayHandle = new System.Windows.Interop.WindowInteropHelper(screenOverlay).Handle;

                //Debug.WriteLine($"[ApplyDimmingAsync] Overlay visible with Handle: {overlayHandle} dim level: {SettingsManager.GetDimmerSliderBrightnessValue()}");
            });
        }
        public async Task UpdateDimLevelAsync(byte newDimLevel)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (screenOverlay != null)
                {
                    screenOverlay.Background = new SolidColorBrush(Color.FromArgb(newDimLevel, 0, 0, 0));
                    //Debug.WriteLine($"[UpdateDimLevelAsync] Updated dimming to level: {newDimLevel}");
                }
            });
        }
        public async Task ResetDimming()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (screenOverlay != null)
                {
                    screenOverlay.Close();
                    screenOverlay = null;  
                    //Debug.WriteLine("[ResetDimming] Dimming canceled, screen brightness restored.");
                }
            });
        }      
        public async Task GetClassHandle()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var installedApps = InstalledAppManager.GetInstalledAppsFromRegistry();
                var topMostZOrderInstalled = TopMostWindowManager.GetInstalledAppWindowsInZOrder(installedApps);
                //Debug.WriteLine($"[GetClassHandle] Z-Order windows count: {topMostZOrderInstalled.Count} Installed apps count: {installedApps.Count}");
                TopMostWindowManager.AdjustZOrder(overlayHandle,installedApps);
            }); 
        }
    




    }
}