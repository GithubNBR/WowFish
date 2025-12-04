using System.Text;
using Dota2Script;
using HookScript;

namespace Dota2Automate;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var autoScript = new AutoClickScript();

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