namespace WindowsDll;

using System;
using System.Drawing;
using System.Runtime.InteropServices;

public class DpiAwareScreenCapture
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, 
                                   int nWidth, int nHeight, 
                                   IntPtr hdcSrc, int nXSrc, int nYSrc, 
                                   int dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("shcore.dll")]
    private static extern int SetProcessDpiAwareness(int value);

    // 设置DPI感知级别
    private enum DpiAwareness
    {
        Unaware = 0,
        SystemAware = 1,
        PerMonitorAware = 2
    }

    /// <summary>
    /// 初始化DPI感知设置
    /// </summary>
    public static void InitializeDpiAwareness()
    {
        try
        {
            // Windows 8.1+ 使用此API
            SetProcessDpiAwareness((int)DpiAwareness.PerMonitorAware);
        }
        catch
        {
            try
            {
                // Windows Vista-8 使用旧API
                SetProcessDPIAware();
            }
            catch { }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    /// <summary>
    /// 获取物理屏幕尺寸（不考虑DPI缩放）
    /// </summary>
    public static Size GetPhysicalScreenSize()
    {
        InitializeDpiAwareness();
        return new Size(
            GetSystemMetrics(SM_CXSCREEN),
            GetSystemMetrics(SM_CYSCREEN));
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    /// <summary>
    /// 捕获全屏（考虑DPI缩放）
    /// </summary>
    public static Bitmap CaptureFullScreen()
    {
        InitializeDpiAwareness();
        Size size = GetPhysicalScreenSize();
        return CaptureScreenRegion(new Rectangle(0, 0, size.Width, size.Height));
    }

    /// <summary>
    /// 捕获屏幕指定区域（考虑DPI缩放）
    /// </summary>
    public static Bitmap CaptureScreenRegion(Rectangle region)
    {
        InitializeDpiAwareness();
        IntPtr desktophWnd = GetDesktopWindow();
        IntPtr desktopDC = GetWindowDC(desktophWnd);
        IntPtr memoryDC = CreateCompatibleDC(desktopDC);
        IntPtr bitmap = CreateCompatibleBitmap(desktopDC, region.Width, region.Height);
        IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

        try
        {
            if (!BitBlt(memoryDC, 0, 0, region.Width, region.Height, 
                       desktopDC, region.X, region.Y, 0x00CC0020)) // SRCCOPY
            {
                throw new System.ComponentModel.Win32Exception();
            }

            Bitmap result = Image.FromHbitmap(bitmap);
            return result;
        }
        finally
        {
            SelectObject(memoryDC, oldBitmap);
            DeleteObject(bitmap);
            DeleteDC(memoryDC);
            ReleaseDC(desktophWnd, desktopDC);
        }
    }
}