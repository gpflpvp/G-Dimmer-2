using System;
using System.Runtime.InteropServices;

public static class VisibilityChecker
{
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    public static List<IntPtr> CheckVisibility(List<IntPtr> topMostWindows)
    {
        var visibleWindows = new List<IntPtr>();

        foreach (var hWnd in topMostWindows)
        {
            if (IsWindowVisible(hWnd))
            {
                visibleWindows.Add(hWnd);
            }
        }

        return visibleWindows;
    }
}