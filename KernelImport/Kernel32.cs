using System.Runtime.InteropServices;

namespace KenerlImport;

public class Kernel32
{
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetModuleHandle(string lpModuleName);
}