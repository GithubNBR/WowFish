using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WowScript
{
    public class HWndInfo
    {
        public IntPtr HWnd { get; set; } = IntPtr.Zero;
        public string Title { get; set; } = string.Empty;
        public uint ProcessId { get; set; }
        public float Scale { get; set; } = 1.0f;


        public override string ToString()
        {
            return $"hwnd: {HWnd}, Title: {Title} (PID: {ProcessId}, Scale: {Scale})";
        }
    }
}
