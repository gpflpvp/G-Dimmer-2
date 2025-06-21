using G_Dimmer_2;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using Application = System.Windows.Application;
using ToolTip = System.Windows.Controls.ToolTip;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace GDimmer
{
    public partial class MainWindow : Window
    {
        private SystemTrayManager systemTrayManager = new SystemTrayManager();
        private DimmerManager dimmerManager = new DimmerManager();
        private static Task? monitorZOrderTask;
        private static Task? monitorSnippingTask;
        private static ManualResetEventSlim monitorZOrderPaused = new(true); 
        private static CancellationTokenSource cts_MonitorZOrderLoop = new();
        private static CancellationTokenSource cts_MonitorSnippingTool = new();
        private ToolTip sliderTooltip = new ToolTip();                                 // Initialized at declaration
        private static bool isMonitoringZOrderLoop = false;                             // Flag to track monitoring state
        private bool isStartUpControl = true;                                          // Flag to track startup control state
        private bool isScreenshotInProgress = false;
        public RoutedCommand ExecuteShortcutCommand { get; } = new RoutedCommand();
        private const int MOD_WIN = 0x0008; // ✅ Windows (Win) key modifier
        private bool isStartUpShortcuts = true; // Flag to track startup shortcuts state


        //initalizations
        public void InitializeSystemTray()
        {
            systemTrayManager.OnOpenGDimmer += OpenGDimmer;
            systemTrayManager.EnableDisableDimmer += EnableDisableDimmer;
            systemTrayManager.OnExit += ExitApplication;
        }
        public void InitializeControlPreviewSetting()
        {
            isStartUpControl = false;
            (PerformanceModeRadioButton.IsChecked, BatterySaverRadioButton.IsChecked) = SettingsManager.GetDimmerMode() ? (true, false) : (false, true);
            DimmerSlider.Value = SettingsManager.GetDimmerSlider();
            IntervalSnipingTool_NumericUpDown.Value = SettingsManager.GetIntervalSnipingTool_Setting();
            MonitoringInterval_NumericUpDown.Value = SettingsManager.GetIntervalTimer();
            sliderTooltip.IsOpen = false;
            Copyright_Label.Content = $"Copyright © 2025 - {(DateTime.Now.Year > 2025 ? DateTime.Now.Year : 2025)} Gerald Flores";
            Screenshots_CheckBox.IsChecked = SettingsManager.GetScreenshotsStutus();
      
        }

        //system tray 
        private void OpenGDimmer()
        {
            if (!this.IsVisible) this.Show();
        }
        private void ExitApplication()
        {
            StopMonitoring();
            systemTrayManager.Dispose();
            Application.Current.Shutdown();
        }
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        //event handlers
        private void BatterySaverRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SettingsManager.SetDimmerMode(false);
            SetBatteryPerformanceControls(false);
            if (SettingsManager.GetStatus()) StopMonitoring();          
        }
        private void PerformanceModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SettingsManager.SetDimmerMode(true);
            SetBatteryPerformanceControls(true);
            if (SettingsManager.GetStatus())
                if (!isMonitoringZOrderLoop) StartMonitoring();
        }
        private async void DimmerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isStartUpControl)
            {
                SettingsManager.SetDimmerSlider((int)DimmerSlider.Value);

                if (SettingsManager.GetStatus())
                {
                    await dimmerManager.UpdateDimLevelAsync(SettingsManager.GetDimmerSliderBrightnessValue());
                }

                sliderTooltip.Content = e.NewValue.ToString("F0");
                sliderTooltip.IsOpen = true;

                double sliderWidth = DimmerSlider.ActualWidth - 50;
                double position = sliderWidth * (DimmerSlider.Value - DimmerSlider.Minimum) / (DimmerSlider.Maximum - DimmerSlider.Minimum);

                sliderTooltip.PlacementTarget = DimmerSlider;
                sliderTooltip.Placement = PlacementMode.Relative;
                sliderTooltip.HorizontalOffset = position - (sliderWidth / 2) + 250;
                sliderTooltip.VerticalOffset = -20;

                DoubleAnimation fadeIn = new DoubleAnimation(0, 2, TimeSpan.FromSeconds(1));
                sliderTooltip.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                await Task.Delay(3000);

                DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1));
                sliderTooltip.BeginAnimation(UIElement.OpacityProperty, fadeOut);

                await Task.Delay(1000);
                sliderTooltip.IsOpen = false;                                          
            }
        }
        private void DimmerButton_Click(object sender, RoutedEventArgs e)
        {
            EnableDisableDimmer();
        }
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);    // Allow only numeric input
        }
        private void MonitoringInterval_NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!isStartUpControl)
            {
                int intervalValue = MonitoringInterval_NumericUpDown.Value ?? 5000;
                SettingsManager.SetIntervalTimer(intervalValue);
             //Debug.WriteLine($"[MonitoringInterval_NumericUpDown_ValueChanged] Interval set to: {intervalValue} ms");
            }
        }
        private void MonitoringInterval_NumericUpDown_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isStartUpControl)
                if (MonitoringInterval_NumericUpDown.Value.ToString() == "" || MonitoringInterval_NumericUpDown.Value < 2000)
                {
                    MonitoringInterval_NumericUpDown.Value = 2000;
                }
                else if (MonitoringInterval_NumericUpDown.Value > 20000)
                    MonitoringInterval_NumericUpDown.Value = 20000;
        }

        private void IntervalSnipingTool_NumericUpDown_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isStartUpControl)
                if (IntervalSnipingTool_NumericUpDown.Value.ToString() == "" || IntervalSnipingTool_NumericUpDown.Value < 1000)
                {
                    IntervalSnipingTool_NumericUpDown.Value = 1000;
                }
                else if (IntervalSnipingTool_NumericUpDown.Value > 10000)
                    IntervalSnipingTool_NumericUpDown.Value = 10000;
        }
        private void IntervalSnipingTool_NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!isStartUpControl)
            {
                int intervalValue = IntervalSnipingTool_NumericUpDown.Value ?? 1000;
                SettingsManager.SetIntervalSnipingTool(intervalValue);
            //Debug.WriteLine($"[IntervalSnipingTool_NumericUpDown_ValueChanged] Interval set to: {intervalValue} ms");
            }
        }

        //event delagatioin
        public async void EnableDisableDimmer()
        {
            if (SettingsManager.ReserseStatus())
            {
                DimmerButton.Content = "Dimmer Enabled";
                await dimmerManager.ApplyDimmingAsync(); 
                if (SettingsManager.GetDimmerMode()) StartMonitoring();
                else
                {
                    if (isMonitoringZOrderLoop) StopMonitoring();
                }
            }
            else
            {
               
                    DimmerButton.Content = "Dimmer Disabled";
                await dimmerManager.ResetDimming(); 
                StopMonitoring();
            }
        }
        private void SetBatteryPerformanceControls(bool value)
        {
            IntervalSnipingTool_NumericUpDown.IsEnabled = value;
            MonitoringInterval_NumericUpDown.IsEnabled = value;
        }
        public async Task MonitorZOrderLoop(CancellationToken token)
        {
            isMonitoringZOrderLoop = true;

            while (!token.IsCancellationRequested)
            {
              //Debug.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] [MonitorZOrder] Zorder Cycle ongoing.");
                monitorZOrderPaused.Wait(); 
                await dimmerManager.GetClassHandle();
                await Task.Delay(TimeSpan.FromMilliseconds(SettingsManager.GetIntervalTimer()), token);
            }
        }
        public async Task MonitorSnippingTool(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
              //Debug.WriteLine("\n[MonitorSnippingTool] Snipping Cycle ongoing");
                if (TopMostWindowManager.GetSnippingToolStatus(DimmerManager.overlayHandle))
                {
                    if (SettingsManager.GetStatus())
                    {
                    //Debug.WriteLine($"[MonitorSnippingTool] Snipping Tool is active.");
                        await dimmerManager.ResetDimming();
                        
                        if (monitorZOrderPaused.IsSet)
                        {
                      //      //Debug.WriteLine($"[MonitorSnippingTool] Pausing ZOrder monitoring.");
                            monitorZOrderPaused.Reset();
                        }
                    }                   
                }
                else
                {
                    if (SettingsManager.GetStatus())
                    {
                        //Debug.WriteLine($"[MonitorSnippingTool] Snipping Tool is not active.");
                        await dimmerManager.ApplyDimmingAsync();

                        if (!monitorZOrderPaused.IsSet)
                        {
                            //Debug.WriteLine($"[MonitorSnippingTool] Resuming ZOrder monitoring.");
                            monitorZOrderPaused.Set();
                        }
                    }
                }
                await Task.Delay(TimeSpan.FromMilliseconds(SettingsManager.GetIntervalSnipingTool_Setting()), token);
            }
        }
        public void StartMonitoring()
        {
            if (monitorZOrderTask == null)
            {
                cts_MonitorZOrderLoop = new CancellationTokenSource();
                monitorZOrderTask = Task.Run(async () => await MonitorZOrderLoop(cts_MonitorZOrderLoop.Token));
            }
            if (monitorSnippingTask == null)
            {
                cts_MonitorSnippingTool = new CancellationTokenSource();
                monitorSnippingTask = Task.Run(async () => await MonitorSnippingTool(cts_MonitorSnippingTool.Token));
            }
        }
        public static void StopMonitoring()
        {
            isMonitoringZOrderLoop = false;

            if (cts_MonitorZOrderLoop != null)
            {
                try{
                    cts_MonitorZOrderLoop.Cancel();
                    monitorZOrderTask = null;
                    cts_MonitorZOrderLoop.Dispose();
                    monitorZOrderPaused.Reset();
                }
                catch(Exception e){
                    //Debug.WriteLine($"[StopMonitoring] Error stopping Zorder monitoring: {e.Message}");
                }
            }
            if (cts_MonitorSnippingTool != null)
            {
                try
                {
                    cts_MonitorSnippingTool.Cancel();
                    monitorSnippingTask = null;
                    cts_MonitorSnippingTool.Dispose();
                }
                catch (Exception e)
                {
                    //Debug.WriteLine($"[StopMonitoring] Error stopping Snipping Tool monitoring: {e.Message}");
                }
               
            }
            //Debug.WriteLine("[StopMonitoring] Monitoring tasks have been stopped.");
        }

        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001; // ✅ Alt modifier
        private const int VK_A = 0x41;      // ✅ A key
        private IntPtr _windowHandle;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainWindow()
        {
            InitializeComponent();
            InitializeSystemTray();

            this.Loaded += (s, e) =>
            {
                //Debug.WriteLine("[MainWindow] Window Loaded Event Triggered!");
                isStartUpShortcuts = false; // ✅ Keep everything except manual execution
            };

            InitializeControlPreviewSetting();
            this.Closing += MainWindow_Closing;
        }

        private void Screenshots_CheckBox_Event(object sender, RoutedEventArgs e)
        {
            if (!isStartUpShortcuts)
            {
                if (Screenshots_CheckBox.IsChecked == true)
                {
                    // ✅ Register Hotkey with a unique ID
                    bool hotkeyRegistered = RegisterHotKey(_windowHandle, 1, MOD_ALT | MOD_WIN, VK_A);

                    if (hotkeyRegistered)
                    {
                        //Debug.WriteLine("[Screenshots_CheckBox_Event] Hotkey registered successfully!");
                    }
                    else
                    {
                        //Debug.WriteLine("[Screenshots_CheckBox_Event] ERROR: Hotkey registration failed. Check if another app is using Win + Alt + A.");
                    }

                    SettingsManager.SetScreenshotsStutus(true);
                    //Debug.WriteLine("[Screenshots_CheckBox_Event] Global Shortcut Enabled!");
                }
                else
                {
                    // ✅ Unregister Hotkey when checkbox is unchecked
                    UnregisterHotKey(_windowHandle, 1);
                    SettingsManager.SetScreenshotsStutus(false);
                    //Debug.WriteLine("[Screenshots_CheckBox_Event] Global Shortcut Disabled!");
                }
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Assign the window handle reliably.
            _windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            //Debug.WriteLine($"[OnSourceInitialized] Window Handle Assigned: {_windowHandle}");

            // Always register the global hotkey Win+Alt+A.
            bool hotkeyRegistered = RegisterHotKey(_windowHandle, 1, MOD_ALT | MOD_WIN, VK_A);
            if (hotkeyRegistered)
            {
                //Debug.WriteLine("[OnSourceInitialized] Global Hotkey (Win+Alt+A) registered successfully.");
            }
            else
            {
                //Debug.WriteLine("[OnSourceInitialized] ERROR: Global Hotkey registration failed. It might be in use by another application.");
            }

            // Attach the global hotkey listener.
            var source = System.Windows.Interop.HwndSource.FromHwnd(_windowHandle);
            source.AddHook(HwndHook);
            //Debug.WriteLine("[OnSourceInitialized] HwndHook Successfully Attached!");
        }
      
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == 1)
            {
                // Only when the dimmer is “on” and we’re not mid-capture
                if (SettingsManager.GetStatus() && !isScreenshotInProgress)
                {
                    isScreenshotInProgress = true;

                    //Debug.WriteLine("[HwndHook] Win+Alt+A pressed.  Turning OFF dimmer…");
                    // This flips your overlay off
                    EnableDisableDimmer();
                    captureModeNow.Visibility= Visibility.Visible;
                    // Schedule the actual capture a hair later so WPF/Windows can repaint
                    _ = Dispatcher.InvokeAsync(async () =>
                    {
                        // let the UI and desktop composition catch up
                        await Task.Delay(150);

                        //Debug.WriteLine("[HwndHook] Capturing screenshot…");
                        var sc = new ScreenCapture();

                        sc.ScreenshotEnded += (s, e) =>
                        {
                            //Debug.WriteLine("[HwndHook] Screenshot complete.  Restoring dimmer…");
                            // turn it back on
                            EnableDisableDimmer();
                            captureModeNow.Visibility = Visibility.Hidden;
                            isScreenshotInProgress = false;
                        };

                        sc.StartScreenshotMode();
                    }, DispatcherPriority.Background)
                    .Task.ContinueWith(t =>
                    {
                        
                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    handled = true;
                }
                else
                {
                    //Debug.WriteLine("[HwndHook] Ignored hotkey (either dimmer is off or a capture is running).");
                }
            }

            return IntPtr.Zero;
        }


    }
}