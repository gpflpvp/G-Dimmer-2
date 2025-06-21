using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.IO;
using System.Diagnostics; // ✅ For Debug Logging

using Point = System.Windows.Point;
using Image = System.Windows.Controls.Image;

using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;
using Rectangle = System.Windows.Shapes.Rectangle;
using GDimmer;
using System.Windows.Threading;

namespace G_Dimmer_2
{
    internal class ScreenCapture
    {
        //   private RenderTargetBitmap fullScreenshot;
        private Point startPoint;
        private Point endPoint;
        private Window? overlayWindow;
        private Image? overlayImage;
        private Rectangle selectionBox;
        public event EventHandler? ScreenshotEnded;
    
        public void StartScreenshotMode()
        {
            //Debug.WriteLine("🚀 Screenshot Mode Started!");
            TakeFullScreenshot();
            ShowDimmedOverlay();
        }
    
        
        private BitmapSource? fullScreenshot; // ✅ Change type from RenderTargetBitmap to BitmapSource

        private void TakeFullScreenshot()
        {
            

            var (totalWidth, totalHeight) = GetPhysicalScreenSize(); // ✅ Get true pixel size

            using (Bitmap bitmap = new Bitmap(totalWidth, totalHeight))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(
                        (int)SystemParameters.VirtualScreenLeft,
                        (int)SystemParameters.VirtualScreenTop,
                        0,
                        0,
                        new System.Drawing.Size(totalWidth, totalHeight)
                    );

                    //Debug.WriteLine($"✅ Full Screenshot Captured: {totalWidth}x{totalHeight} (Scaled Correctly)");
                }

                BitmapSource bmpSource = ConvertBitmapToBitmapSource(bitmap);

                if (bmpSource == null)
                {
                    //Debug.WriteLine("❌ ERROR: BitmapSource conversion failed!");
                    return;
                }

                fullScreenshot = bmpSource;

                // ✅ Copy to clipboard immediately after capturing the screenshot
               // CopyToClipboard(fullScreenshot);
            }
        }


        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            //Debug.WriteLine("🔄 Converting Bitmap to WPF BitmapSource (Raw)...");

            if (bitmap == null)
            {
                //Debug.WriteLine("❌ ERROR: Bitmap input is null! Cannot convert.");
                return null;
            }

            using (MemoryStream memory = new MemoryStream())
            {
                try
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                    memory.Position = 0;

                    // ⚡ Ensure no interpolation/stretching occurs
                    BitmapSource bmpSource = BitmapFrame.Create(memory, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

                    if (bmpSource == null)
                    {
                        //Debug.WriteLine("❌ ERROR: BitmapSource conversion failed!");
                        return null;
                    }

                    //Debug.WriteLine("✅ Raw image conversion successful!");
                    return bmpSource;
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine($"❌ ERROR: Exception in Bitmap conversion - {ex.Message}");
                    return null;
                }
            }
        }
        private void ShowDimmedOverlay()
        {
            //Debug.WriteLine("🌒 Showing Dimmed Overlay...");

            if (fullScreenshot == null)
            {
                //Debug.WriteLine("❌ ERROR: fullScreenshot is null! Overlay cannot be created.");
                return;
            }

            overlayWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Maximized,
                Topmost = true,
                Background = Brushes.Black,
                AllowsTransparency = false
            };

            overlayImage = new Image
            {
                Source = fullScreenshot,
                Stretch = Stretch.None
            };

            selectionBox = new Rectangle
            {
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }, // ✅ Adds a dashed outline
                Visibility = Visibility.Hidden
            };

            Canvas canvas = new Canvas();

            canvas.Children.Add(overlayImage); // ✅ Background image
            canvas.Children.Add(selectionBox); // ✅ Selection box overlay

            overlayWindow.Content = canvas; // ✅ Keep canvas, do not overwrite

            overlayWindow.MouseDown += CaptureStart;
            overlayWindow.MouseMove += CaptureMove; // ✅ Updates rectangle dynamically
            overlayWindow.MouseUp += CaptureEnd;

            overlayWindow.Show();
            //Debug.WriteLine("✅ Overlay Window Displayed Without Transparency.");
        }
        private void CaptureMove(object sender, System.Windows.Input.MouseEventArgs e) // ✅ Explicit namespace
        {
            if (selectionBox.Visibility != Visibility.Visible) return;

            Point currentPoint = e.GetPosition(overlayWindow);

            if (e.LeftButton == MouseButtonState.Pressed) // ✅ Only update if dragging
            {
                double x = Math.Min(startPoint.X, currentPoint.X);
                double y = Math.Min(startPoint.Y, currentPoint.Y);
                double width = Math.Abs(currentPoint.X - startPoint.X);
                double height = Math.Abs(currentPoint.Y - startPoint.Y);

                // ✅ Update selection box dimensions dynamically
                Canvas.SetLeft(selectionBox, x);
                Canvas.SetTop(selectionBox, y);
                selectionBox.Width = width;
                selectionBox.Height = height;
            }
        }

        private double GetDpiScalingFactor()
        {
            PresentationSource source = PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow);
            return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        }
        private (int Width, int Height) GetPhysicalScreenSize()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return ((int)(SystemParameters.VirtualScreenWidth * g.DpiX / 96),
                        (int)(SystemParameters.VirtualScreenHeight * g.DpiY / 96));
            }
        }
        private void CaptureStart(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(overlayWindow);
            //Debug.WriteLine($"🎯 Selection Started at: {startPoint.X}, {startPoint.Y}");

            selectionBox.Visibility = Visibility.Visible;
            Canvas.SetLeft(selectionBox, startPoint.X);
            Canvas.SetTop(selectionBox, startPoint.Y);
        }



        private void CropSelection()
        {
            if (fullScreenshot == null)
            {
                //Debug.WriteLine("❌ ERROR: fullScreenshot is null! Cannot crop.");
                return;
            }

            //Debug.WriteLine("✂ Cropping Selected Area...");

            double scaleFactor = GetDpiScalingFactor(); // ✅ Detect DPI scaling

            // ✅ Convert selection coordinates to match actual screen pixels
            int x = Math.Max(0, (int)(Math.Min(startPoint.X, endPoint.X) * scaleFactor));
            int y = Math.Max(0, (int)(Math.Min(startPoint.Y, endPoint.Y) * scaleFactor));
            int width = Math.Min((int)(Math.Abs(endPoint.X - startPoint.X) * scaleFactor), fullScreenshot.PixelWidth - x);
            int height = Math.Min((int)(Math.Abs(endPoint.Y - startPoint.Y) * scaleFactor), fullScreenshot.PixelHeight - y);

            //Debug.WriteLine($"🔍 Cropping at Adjusted Coordinates: X={x}, Y={y}, Width={width}, Height={height}");

            // Prevent invalid selections
            if (width <= 0 || height <= 0)
            {
                //Debug.WriteLine("⚠ Invalid Crop Selection: No area selected.");
                return;
            }

            try
            {
                //Debug.WriteLine("✂ Creating Cropped Bitmap...");
                CroppedBitmap croppedImage = new CroppedBitmap(fullScreenshot, new Int32Rect(x, y, width, height));

                //Debug.WriteLine("📋 Copying Cropped Image to Clipboard...");
                Clipboard.SetImage(croppedImage);

                //Debug.WriteLine("✅ Cropped Screenshot copied successfully!");
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"❌ ERROR: Exception while cropping - {ex.Message}");
            }
        }



        private void CaptureEnd(object sender, MouseButtonEventArgs e)
        {
            endPoint = e.GetPosition(overlayWindow);
            //Debug.WriteLine($"🏁 Selection Ended at: {endPoint.X}, {endPoint.Y}");

            // Adjust selection box dimensions dynamically
            selectionBox.Width = Math.Abs(endPoint.X - startPoint.X);
            selectionBox.Height = Math.Abs(endPoint.Y - startPoint.Y);

            CropSelection();

            if (overlayWindow != null)
            {
                // Attach to the Closed event so that we can raise our event after the window is closed.
                overlayWindow.Closed += (s, args) =>
                {
                    // Notify subscribers that the screenshot process is complete.
                    ScreenshotEnded?.Invoke(this, EventArgs.Empty);
                };

                // Close the overlay window.
                overlayWindow.Close();
            }
        }



        private void CopyToClipboard(BitmapSource bitmap)
        {
            //Debug.WriteLine("📋 Copying Screenshot to Clipboard...");
            Clipboard.SetImage(bitmap);
        }
    }
}