using Epic7SecretShopAutoBuyer.Models;
using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);


    const int SW_RESTORE = 9;


    // Mouse event constants
    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;

    static void Main()
    {
        while (true)
        {
            // Step 1: Capture full screen as Bitmap
            Bitmap screenBmp = CaptureScreen();
            // Step 2: Convert screen capture to Mat and grayscale
            Mat screenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenBmp);
            Cv2.CvtColor(screenMat, screenMat, ColorConversionCodes.BGR2GRAY);
            // Step 3: Load the target image and convert to grayscale
            string refreshImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "RefreshButton.png");
            if (!File.Exists(refreshImagePath))
            {
                Console.WriteLine("Template image not found: " + refreshImagePath);
            }
            Mat refreshTemplate = Cv2.ImRead(refreshImagePath, ImreadModes.Grayscale);
            BestMatchLocation bookmarkImageBML = GetBestMatchLocation(screenMat, refreshTemplate);
            Move(bookmarkImageBML, refreshTemplate);


            Thread.Sleep(2000);
            ForceActivateEpicSeven();

            string confirmImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Confirm.png");
            if (!File.Exists(confirmImagePath))
            {
                Console.WriteLine("Template image not found: " + confirmImagePath);
            }
            Mat confirmTemplate = Cv2.ImRead(confirmImagePath, ImreadModes.Grayscale);
            Bitmap screenBmpAfterRefresh = CaptureScreen();
            Mat screenBmpAfterRefreshMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenBmpAfterRefresh);
            Cv2.CvtColor(screenBmpAfterRefreshMat, screenBmpAfterRefreshMat, ColorConversionCodes.BGR2GRAY);
            BestMatchLocation confirmImageBML = GetBestMatchLocation(screenBmpAfterRefreshMat, confirmTemplate);
            Move(confirmImageBML, confirmTemplate);

            Thread.Sleep(2000);
            ForceActivateEpicSeven();

            string bookmarkImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Bookmark.png");
            string secretMedalImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "SecretMedal.png");

            if (!File.Exists(bookmarkImagePath))
            {
                Console.WriteLine("Template image not found: " + bookmarkImagePath);
            }
            if (!File.Exists(secretMedalImagePath))
            {
                Console.WriteLine("Template image not found: " + secretMedalImagePath);
            }

            Mat bookmarkTemplate = Cv2.ImRead(bookmarkImagePath, ImreadModes.Grayscale);
            Mat secretMedalTemplate = Cv2.ImRead(secretMedalImagePath, ImreadModes.Grayscale);
            Bitmap screenBmpFinishRefresh = CaptureScreen();
            Mat screenBmpFinishRefreshMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenBmpFinishRefresh);
            Cv2.CvtColor(screenBmpFinishRefreshMat, screenBmpFinishRefreshMat, ColorConversionCodes.BGR2GRAY);
            BestMatchLocation bookmarkBML = GetBestMatchLocation(screenBmpFinishRefreshMat, bookmarkTemplate);
            BestMatchLocation secretMedalBML = GetBestMatchLocation(screenBmpFinishRefreshMat, secretMedalTemplate);

            if (bookmarkBML != null)
            {
                Move(bookmarkBML, bookmarkTemplate);
                ForceActivateEpicSeven();

            }

            if (secretMedalBML != null)
            {
                Move(secretMedalBML, secretMedalTemplate);
                ForceActivateEpicSeven();

            }
            //DrawRectangle(bookmarkImageBML, screenMat, template);
        }
    }


    static Bitmap CaptureScreen()
    {
        Rectangle bounds = Screen.PrimaryScreen.Bounds;
        Bitmap bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
        }
        return bmp;
    }

    static BestMatchLocation GetBestMatchLocation(Mat screenMat, Mat template)
    {
        // Step 4: Template matching
        Mat result = new Mat();
        Cv2.MatchTemplate(screenMat, template, result, TemplateMatchModes.CCoeffNormed);

        // Step 5: Get best match location
        Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);
        Console.WriteLine($"Best match at: {maxLoc} with confidence: {maxVal}");

        return new BestMatchLocation() { MaxLoc = maxLoc, MinLoc = minLoc , MaxVal = maxVal, MinVal = minVal};
    }

    static void Move(BestMatchLocation bestMatchLocation, Mat template)
    {
        if (bestMatchLocation.MaxVal >= 0.9)
        {
            // Center of the matched rectangle
            int clickX = bestMatchLocation.MaxLoc.X + template.Width / 2;
            int clickY = bestMatchLocation.MaxLoc.Y + template.Height / 2;

            Console.WriteLine($"Clicking at ({clickX}, {clickY})");

            // Move the mouse
            SetCursorPos(clickX, clickY);
            Thread.Sleep(100); // 100 ms delay to let the cursor move

        //    // Click
        //    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        //    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }
    }

    static void DrawRectangle(BestMatchLocation bestMatchLocation,Mat screenMat , Mat template)
    {
        if (bestMatchLocation.MaxVal >= 0.9)
        {
            // Draw a rectangle on the match
            Rect matchRect = new Rect(bestMatchLocation.MaxLoc.X, bestMatchLocation.MaxLoc.Y, template.Width, template.Height);
            Cv2.Rectangle(screenMat, matchRect, Scalar.White, 2);

            // Show result
            Cv2.ImShow("Match Result", screenMat);
            Cv2.WaitKey();
        }
        else
        {
            Console.WriteLine("No good match found.");
        }
    }

    static bool ForceActivateEpicSeven()
    {
        Process[] processes = Process.GetProcessesByName("ApplicationFrameHost");
        if (processes.Length == 0)
        {
            Console.WriteLine("EpicSeven process not found.");
            return false;
        }

        IntPtr hWnd = processes[0].MainWindowHandle;
        if (hWnd == IntPtr.Zero)
        {
            Console.WriteLine("EpicSeven window handle not found.");
            return false;
        }

        // Restore if minimized
        ShowWindow(hWnd, SW_RESTORE);

        // Trick: Attach input threads to allow SetForegroundWindow to succeed
        IntPtr currentForeground = GetForegroundWindow();
        uint foregroundThread = GetWindowThreadProcessId(currentForeground, out _);
        uint thisThread = GetCurrentThreadId();

        AttachThreadInput(thisThread, foregroundThread, true);
        bool success = SetForegroundWindow(hWnd);
        AttachThreadInput(thisThread, foregroundThread, false);

        return success;
    }





}
