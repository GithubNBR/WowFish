using System.Runtime.InteropServices;

namespace WindowsDll;

public class ScreenCapture
{
    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(IntPtr hWnd, out Point lpPoint);
    
    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hwnd);
    
    public static RECT GetWindowRectangle(IntPtr hWnd)
    {
        if (!GetWindowRect(hWnd, out RECT rect))
            throw new System.ComponentModel.Win32Exception();
        return rect;
    }
    
    
    /// <summary>
    /// 捕获整个窗口
    /// </summary>
    public static Bitmap CaptureWindow(IntPtr hWnd)
    {
        var rect = GetWindowRectangle(hWnd);
        var dpi = GetDpiForWindow(hWnd);
        var scaleFactor = dpi / 96.0f; // 96 DPI 是标准DPI
        
        return CaptureScreenRegion(rect.Left, rect.Top, rect.Width, rect.Height, scaleFactor);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="scaleFactor">放大因子，根据dpi计算</param>
    /// <returns></returns>
    public static Bitmap CaptureScreenRegion(int x, int y, int w, int h, float scaleFactor = 1.0f)
    {
        Bitmap bmp = new Bitmap((int)(w * scaleFactor), (int)(h * scaleFactor));
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen((int)(x * scaleFactor), (int)(y * scaleFactor), 0, 0, 
                new Size((int)(w * scaleFactor), (int)(h * scaleFactor)));
        }
        
        // bmp.Save($"screenshot/screen{DateTime.Now.Ticks}.png", System.Drawing.Imaging.ImageFormat.Png);
        
        return bmp;
    }
    
    
    public static Bitmap CaptureScreenRegion(RECT region, float scaleFactor = 1.0f)
    {
        return CaptureScreenRegion(region.Left, region.Top, region.Width, region.Height, scaleFactor);
    }
    
    
    /// <summary>
    /// 捕获窗口客户区(不包括边框和标题栏)
    /// </summary>
    public static Bitmap CaptureWindowClientArea(IntPtr hWnd)
    {
        GetClientRect(hWnd, out Rectangle clientRect);
        ClientToScreen(hWnd, out Point clientTopLeft);
        
        var dpi = GetDpiForWindow(hWnd);
        var scaleFactor = dpi / 96.0f; // 96 DPI 是标准DPI
        
        return CaptureScreenRegion(clientTopLeft.X, clientTopLeft.Y, 
            clientRect.Width, clientRect.Height, scaleFactor);
    }
}