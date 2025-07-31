namespace WindowsDll;

using System;
using System.Drawing;
using System.Runtime.InteropServices;

public class WindowCapture
{
    // 导入必要的 Windows API
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth,
        int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    
    /// <summary>
    /// 获取窗口的矩形区域
    /// </summary>
    public static RECT GetWindowRectangle(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            throw new ArgumentException("无效的窗口句柄");

        if (!GetWindowRect(hWnd, out RECT rect))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

        return rect;
    }
    
    
        /// <summary>
    /// 捕获指定窗口的截图（即使不在最前端）
    /// </summary>
    public static Bitmap CaptureWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            throw new ArgumentException("无效的窗口句柄");

        // 获取窗口矩形
        RECT rect = GetWindowRectangle(hWnd);
        
        // 创建位图
        Bitmap bmp = new Bitmap(rect.Width, rect.Height);
        
        using (Graphics g = Graphics.FromImage(bmp))
        {
            IntPtr hdc = g.GetHdc();
            
            try
            {
                // 使用 PrintWindow API 捕获窗口内容
                if (!PrintWindow(hWnd, hdc, 0))
                {
                    // 如果 PrintWindow 失败，尝试备用方法
                    IntPtr windowDc = GetWindowDC(hWnd);
                    if (windowDc != IntPtr.Zero)
                    {
                        try
                        {
                            if (!BitBlt(hdc, 0, 0, rect.Width, rect.Height, 
                                windowDc, 0, 0, 0x00CC0020)) // SRCCOPY
                            {
                                throw new System.ComponentModel.Win32Exception(
                                    Marshal.GetLastWin32Error());
                            }
                        }
                        finally
                        {
                            ReleaseDC(hWnd, windowDc);
                        }
                    }
                }
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }

        return bmp;
    }

    /// <summary>
    /// 更可靠的窗口捕获方法（使用内存DC）
    /// </summary>
    public static Bitmap CaptureWindowEx(IntPtr hWnd)
    {
        RECT rect = GetWindowRectangle(hWnd);
        
        IntPtr windowDc = GetWindowDC(hWnd);
        if (windowDc == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

        IntPtr memoryDc = CreateCompatibleDC(windowDc);
        if (memoryDc == IntPtr.Zero)
        {
            ReleaseDC(hWnd, windowDc);
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        IntPtr hBitmap = CreateCompatibleBitmap(windowDc, rect.Width, rect.Height);
        if (hBitmap == IntPtr.Zero)
        {
            DeleteDC(memoryDc);
            ReleaseDC(hWnd, windowDc);
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        IntPtr oldBitmap = SelectObject(memoryDc, hBitmap);
        
        try
        {
            if (!BitBlt(memoryDc, 0, 0, rect.Width, rect.Height, 
                windowDc, 0, 0, 0x00CC0020)) // SRCCOPY
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            Bitmap bmp = Image.FromHbitmap(hBitmap);
            return bmp;
        }
        finally
        {
            SelectObject(memoryDc, oldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(memoryDc);
            ReleaseDC(hWnd, windowDc);
        }
    }
}