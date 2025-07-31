using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoScript;
using KenerlImport;

namespace HookScript;

// 简单的键盘钩子类(用于监听热键)
public class KeyboardHookHelper
{
    
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    
    private User32.KeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;

    public event EventHandler<KeyEventArgs>? KeyPressed;

    public void SetHook()
    {
        _proc = HookCallback;
        _hookID = SetHook(_proc);
    }

    private IntPtr SetHook(User32.KeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        
        using var curModule = curProcess.MainModule;
        
        return User32.SetWindowsHookEx(WH_KEYBOARD_LL, proc, Kernel32.GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            
            KeyPressed?.Invoke(this, new KeyEventArgs(vkCode));
        }
        
        return User32.CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    public void Unhook()
    {
        User32.UnhookWindowsHookEx(_hookID);
    }
}