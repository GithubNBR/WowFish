using System.Runtime.InteropServices;

namespace WindowsDll;

// 矩形结构体
[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
    public Rectangle ToRectangle() => new Rectangle(Left, Top, Width, Height);
}