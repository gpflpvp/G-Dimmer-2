using System;
using System.Diagnostics;
using System.Security.Principal;

namespace GDimmer
{
    public static class SettingsManager
    {
        private static bool status = false;

        //Properties Settings
        public static void SetIntervalTimer(int value)
        {
            G_Dimmer_2.Properties.Settings.Default.IntervalZOrder_Setting = value;
            G_Dimmer_2.Properties.Settings.Default.Save();
        }

        public static int GetIntervalTimer()
        {
            return G_Dimmer_2.Properties.Settings.Default.IntervalZOrder_Setting;
        }

        public static void SetDimmerSlider(int value)
        {
            G_Dimmer_2.Properties.Settings.Default.DimmerSlider_Setting = value;
            G_Dimmer_2.Properties.Settings.Default.Save();
        }

        public static int GetDimmerSlider()
        {
            return G_Dimmer_2.Properties.Settings.Default.DimmerSlider_Setting;
        }
        public static byte GetDimmerSliderBrightnessValue()
        {
            return (byte)(G_Dimmer_2.Properties.Settings.Default.DimmerSlider_Setting * 255 / 100);
        }
        public static void SetDimmerMode(bool value)
        {
            G_Dimmer_2.Properties.Settings.Default.DimmerMode_Setting = value;
            G_Dimmer_2.Properties.Settings.Default.Save();
        }
        public static bool GetDimmerMode()
        {
            return G_Dimmer_2.Properties.Settings.Default.DimmerMode_Setting;
        }
        public static void SetIntervalSnipingTool(int value)
        {
            G_Dimmer_2.Properties.Settings.Default.IntervalSnipingTool_Setting = value;
            G_Dimmer_2.Properties.Settings.Default.Save();
        }
        public static int GetIntervalSnipingTool_Setting()
        {
            return G_Dimmer_2.Properties.Settings.Default.IntervalSnipingTool_Setting;
        }

        public static void SetScreenshotsStutus(bool value)
        {
            G_Dimmer_2.Properties.Settings.Default.Screenshots_Setting = value;
            G_Dimmer_2.Properties.Settings.Default.Save();
        }
        public static bool GetScreenshotsStutus()
        {
            return G_Dimmer_2.Properties.Settings.Default.Screenshots_Setting;
        }


        //App Status Settings
        public static bool ReserseStatus()
        {
            return status = !status;
        }
        public static bool GetStatus()
        {
            return status;
        }
        public static bool IsRunningAsAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                //Debug.WriteLine($"[IsRunningAsAdmin] Application running as admin: {isAdmin}");
                return isAdmin;
            }
        }
    }
}