using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

public static class TopMostWindowManager
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxLength);
    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    private static List<(int index, IntPtr hWnd, string className, string windowTitle)> zOrderWindows = new();
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private const uint SWP_NOACTIVATE = 0x0010;
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001; 
    private const uint SWP_NOMOVE = 0x0002;

    public static List<(int index, IntPtr hWnd, string className, string windowTitle)> GetInstalledAppWindowsInZOrder(List<string> installedAppPaths)
    {
        var allWindows = EnumerateWindows();
        return FilterWindows(allWindows, installedAppPaths);
    }
    private static List<(IntPtr hWnd, string className, string windowTitle)> EnumerateWindows()
    {
        var windows = new List<(IntPtr, string, string)>();

        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd)) return true;

            var className = GetWindowClassName(hWnd);
            var windowTitle = GetWindowText(hWnd);

            windows.Add((hWnd, className, windowTitle));
            return true;
        }, IntPtr.Zero);

        return windows;
    }
    private static string GetWindowClassName(IntPtr hWnd)
    {
        var classNameBuilder = new StringBuilder(256);
        GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
        return classNameBuilder.ToString();
    }
    private static string GetWindowText(IntPtr hWnd)
    {
        var windowTitleBuilder = new StringBuilder(256);
        GetWindowText(hWnd, windowTitleBuilder, windowTitleBuilder.Capacity);
        return windowTitleBuilder.ToString();
    }
    private static List<(int index, IntPtr hWnd, string className, string windowTitle)> FilterWindows(List<(IntPtr hWnd, string className, string windowTitle)> allWindows,List<string> installedAppPaths)
    {
        var filteredWindows = new List<(int index, IntPtr hWnd, string className, string windowTitle)>();

        for (int i = 0; i < allWindows.Count; i++)
        {
            var (hWnd, className, windowTitle) = allWindows[i];
            GetWindowThreadProcessId(hWnd, out uint processId);

            if (IsProcessPathMatching(processId, installedAppPaths))
            {
                filteredWindows.Add((i, hWnd, className, windowTitle));
            }
        }

        return filteredWindows;
    }
    private static bool IsProcessPathMatching(uint processId, List<string> installedAppPaths)
    {
        try
        {
            Process process = Process.GetProcessById((int)processId);
            string? exePath = process.MainModule?.FileName;

            if (!string.IsNullOrEmpty(exePath))
            {
                foreach (var appPath in installedAppPaths)
                {
                    if (exePath.StartsWith(appPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch (System.ComponentModel.Win32Exception){ }

        return false;
    }
    public static List<(int index, IntPtr hWnd, string className, string windowTitle)> GetZOrderedWindows()
    {
        var allWindows = EnumerateWindows();
        var zOrderWindows = allWindows.Select((window, i) => (i, window.hWnd, window.className, window.windowTitle)).ToList();

       // Debug.WriteLine($"[GetAllZOrderWindows] Listing all detected windows in Z-order with Count: {zOrderWindows.Count}");

        return zOrderWindows;
    }
    public static void AdjustZOrder(IntPtr overlayHandle, List<string> installedAppPaths)
    {
        zOrderWindows = EnumerateWindows().Select((window, i) => (i, window.hWnd, window.className, window.windowTitle)).ToList();

        int overlayIndex = zOrderWindows.FindIndex(win => win.hWnd == overlayHandle);
        if (overlayIndex == -1)
        {
          //  Debug.WriteLine("[AdjustZOrder] Overlay not found in Z-order list.");
            return;
        }

       // Debug.WriteLine($"[AdjustZOrder] Overlay found at index: {overlayIndex}");

        foreach (var (index, hWnd, className, windowTitle) in zOrderWindows)
        {
            if (!TryGetProcessPath(hWnd, out string? exePath) || exePath == null) continue;

            bool isInstalledApp = installedAppPaths.Any(appPath => exePath.StartsWith(appPath, StringComparison.OrdinalIgnoreCase));
            if (isInstalledApp && index < overlayIndex)
            {
              //  Debug.WriteLine($"[AdjustZOrder] Moving {windowTitle} (Handle: {hWnd}) behind the overlay.");
                SetTopMost(hWnd, overlayHandle);
            }
        }
    }
    private static bool TryGetProcessPath(IntPtr hWnd, out string? exePath)
    {
        exePath = null;
        GetWindowThreadProcessId(hWnd, out uint processId);

        try
        {
            exePath = Process.GetProcessById((int)processId)?.MainModule?.FileName;
            return !string.IsNullOrEmpty(exePath);
        }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }
    public static bool GetSnippingToolStatus(IntPtr overlayHandle)
    {
        zOrderWindows = GetZOrderedWindows();

            int overlayIndex = zOrderWindows.FindIndex(win => win.hWnd == overlayHandle);
            int classCaptureScreen = zOrderWindows.FindIndex(win => win.className == "SnipOverlayRootWindow");
            int classRecordingScreen = zOrderWindows.FindIndex(win => win.className == "RecordingAreaIndicatorWindow");

           // Debug.WriteLine($"[GetSnippingToolStatus] Overlay index: {overlayIndex}, CaptureScreen index: {classCaptureScreen}, RecordingScreen index: {classRecordingScreen}");

            if (classCaptureScreen != -1 || classRecordingScreen != -1)
            {
                return true;
            }      
        return false;
    }
    public static void SetTopMost(IntPtr hWnd, IntPtr overlayHandle)
    {
        SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOSIZE | SWP_NOMOVE);
        SetWindowPos(hWnd, overlayHandle, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOSIZE | SWP_NOMOVE);
    }
}