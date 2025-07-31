using System.Text;
using AutoScript;
using HookScript;
using WowScript;

namespace Fishing;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        IAutoScript autoScript = new FishScript();

        // 设置热键监听
        var hook = new KeyboardHookHelper();
        hook.KeyPressed += (sender, e) =>
        {
            if (e.KeyCode == Settings.Instance.StartKey)
            {
                Task.Run(async () => await autoScript.StartAsync());
            }
            else if (e.KeyCode == Settings.Instance.StopKey)
            {
                Task.Run(async () => await autoScript.StopAsync());
            }
        };
        
        hook.SetHook();
        Application.Run(new MainForm());
        hook.Unhook();
    }
}