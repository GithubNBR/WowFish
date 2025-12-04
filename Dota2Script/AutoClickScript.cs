using AutoScript;
using KenerlImport;
using LogDll;
using WindowsDll;

namespace Dota2Script;

public class AutoClickScript : IAutoScript
{
    public string Name { get; } = "Dota2 自动点击";
    protected bool _isRunning = false;
    public bool IsRunning => _isRunning;
    public object ScriptData => new object();
    
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        _isRunning = true;
        LogHelper.Info($"{Name} 开始");
        
        while (_isRunning)
        {
            // await RecordPoint();

            await AutoProcess();
        }
    }

    public async Task StopAsync()
    {
        await Task.Yield();
        _isRunning = false;
        LogHelper.Info($"{Name} 结束");
    }

    /// <summary>
    /// 获取当前的鼠标位置
    /// </summary>
    private async Task RecordPoint()
    {
        User32.GetCursorPos(out var currentPoint);
        LogHelper.Info($"当前的鼠标位置是 X: {currentPoint.X}, Y: {currentPoint.Y}");
        await Task.Delay(1000, CancellationToken.None);
    }


    /// <summary>
    /// 解封胶囊 自动处理过程
    /// </summary>
    private async Task AutoProcess()
    {
        /// 解封胶囊 当前的鼠标位置是 X: 1469, Y: 841
        /// 跳过    当前的鼠标位置是 X: 1290, Y: 1237
        /// 完成    当前的鼠标位置是 X: 1273, Y: 1057

        
        await ClickAndWait(1469, 841);
        await ClickAndWait(1290, 1237);
        await ClickAndWait(1273, 1057);
    }

    private async Task ClickAndWait(int x, int y)
    {
        User32.SetCursorPos(x, y);
        await Task.Delay(100);
        User32.mouse_event(InputEventConstant.MouseLeftDown, 0, 0, 0, 0);
        await Task.Delay(100); // 等待点击效果
        User32.mouse_event(InputEventConstant.MouseLeftUp, 0, 0, 0, 0);
        await Task.Delay(1000);
    }
}