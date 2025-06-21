using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public static class InstalledAppManager
{
    public static List<string> GetInstalledAppsFromRegistry()
    {
        var installedApps = new List<string>();

        string registryPath64 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        string registryPath32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

        try
        {
            installedApps.AddRange(GetInstalledAppsFromPath(registryPath64, RegistryView.Registry64));
            installedApps.AddRange(GetInstalledAppsFromPath(registryPath32, RegistryView.Registry32));

         //    var systemApps = GetSystemApps();
         //   installedApps.AddRange(systemApps);
        }
        catch (Exception ex)
        {
            //Debug.WriteLine($"❌ [GetInstalledAppsFromRegistry] Error: {ex.Message}");
        }

        return installedApps;
    }

    private static List<string> GetInstalledAppsFromPath(string registryPath, RegistryView registryView)
    {
        var apps = new List<string>();

        using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
        using (RegistryKey? key = baseKey.OpenSubKey(registryPath))
        {
            if (key == null)
            {
             //   Console.WriteLine($"[GetInstalledAppsFromPath] Registry path '{registryPath}' could not be opened.");
                return apps;
            }

            foreach (string subKeyName in key.GetSubKeyNames())
            {
                using (RegistryKey? subKey = key.OpenSubKey(subKeyName))
                {
                    string? displayName = subKey?.GetValue("DisplayName")?.ToString();
                    string? exePath = subKey?.GetValue("InstallLocation")?.ToString();

                    if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(exePath))
                    {
                        apps.Add(exePath);
                    }
                }
            }
        }

        return apps;
    }
}