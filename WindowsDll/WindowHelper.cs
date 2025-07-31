using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsDll;

public class WindowHelper
{
    /// <summary>
    /// 获取当前活动窗口的句柄
    /// </summary>
    /// <returns></returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
    
    /// <summary>
    /// 获取窗口句柄的标题
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="lpString"></param>
    /// <param name="nMaxCount"></param>
    /// <returns></returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    
    
    /// <summary>
    /// 获取窗口句柄的进程ID
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="lpdwProcessId"></param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    
    
    /// <summary>
    /// 附加到进程上
    /// </summary>
    /// <param name="idAttach"></param>
    /// <param name="idAttachTo"></param>
    /// <param name="fAttach"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    
    
    /// <summary>
    /// 获取当前活动窗口的句柄
    /// </summary>
    /// <returns></returns>
    public static IntPtr GetActiveWindowHandle()
    {
        return GetForegroundWindow();
    }
    
    /// <summary>
    /// 获取窗口句柄的标题
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns></returns>
    public static string GetWindowTitle(IntPtr hWnd)
    {
        const int nChars = 256;
        StringBuilder Buff = new StringBuilder(nChars);
        if (GetWindowText(hWnd, Buff, nChars) > 0)
        {
            return Buff.ToString();
        }
        return null;
    }
    
    /// <summary>
    /// 获取窗口句柄的进程ID
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns></returns>
    public static uint GetWindowProcessId(IntPtr hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint processId);
        return processId;
    }
    
    
    // 辅助方法：通过进程名查找窗口句柄
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    
    public static IntPtr FindWindowByProcessName(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        return processes.Length > 0 ? processes[0].MainWindowHandle : IntPtr.Zero;
    }
}