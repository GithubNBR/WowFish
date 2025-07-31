using System;
using AutoScript;
using KenerlImport;
using LogDll;
using WindowsDll;

namespace WowScript;

public class FishScript : IAutoScript
{

    #region 成员变量

    private volatile bool _isRunning;

    private const int FishingKey = 0x32; // 钓鱼按键数字2,此键是Windows虚拟按钮，与AscII码不同

    private HWndInfo? _hwndInfo;

    #endregion

    public string Name => "自动钓鱼";

    public bool IsRunning => _isRunning;


    public object ScriptData => new object();

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;
        _isRunning = true;
        LogHelper.Info($"{Name}开始...");

        if (!InitHWndInfo())
        {
            _isRunning = false;
            LogHelper.Info($"{Name} 初始化窗口失败，请重新尝试");
            return;
        }

        while(_isRunning)
        {
            await FishingProcess();
        }

    }

    private async Task FishingProcess()
    {
        try
        {
            var utc = DateTime.Now.AddSeconds(30).ToFileTimeUtc();
            LogHelper.Info($"开始钓鱼流程..., 本次钓鱼结束时间最迟为 {utc}");
            // 1. 施放钓鱼技能
            await CastFishing();
            
            var rect = WindowCapture.GetWindowRectangle(_hwndInfo!.HWnd);
            LogHelper.Info($"窗口位置: {rect.Left},{rect.Top} 大小: {rect.Width}x{rect.Height}");

            // 2. 查找鱼漂位置
            if (FindBobber(rect, out var lastPos))
            {
                // 3 等待鱼上钩
                if (await DetectBobberBite(rect, lastPos, utc))
                {
                    // 4 收竿
                    await ReelIn(lastPos, rect);
                }
                else
                {
                    LogHelper.Info($"超时未检测到鱼上钩");
                }
            }
            else
            {
                LogHelper.Info("未检测到鱼漂....");
            }

            // 等待随机时间
            // LogHelper.Info($"随机等待3~8秒，准备下一次钓鱼...");
            // await Task.Delay(new Random().Next(3000, 8000));
        }
        catch (Exception ex)
        {
            LogHelper.Info($"发生错误: {ex.Message}");
            await Task.Delay(5000);
        }
    }

    public async Task StopAsync()
    {
        await Task.Yield();
        _isRunning = false;
        LogHelper.Info($"{Name}结束!");
    }




    #region 过程

    /// <summary>
    /// 初始化程序句柄
    /// </summary>
    /// <returns></returns>
    private bool InitHWndInfo()
    {
        var hWnd = new HWndInfo();
        var activeWindowHandle = WindowHelper.GetActiveWindowHandle();
        if (activeWindowHandle == IntPtr.Zero)
        {
            LogHelper.Info("未检测到活动窗口，请确保游戏窗口处于前台。");
            return false;
        }

        hWnd.HWnd = activeWindowHandle;
        hWnd.Title = WindowHelper.GetWindowTitle(activeWindowHandle);
        hWnd.ProcessId = WindowHelper.GetWindowProcessId(activeWindowHandle);
        var dpiForWindow = ScreenCapture.GetDpiForWindow(activeWindowHandle);
        var scaleFactor = dpiForWindow / 96.0f; // 96 DPI 是标准DPI
        hWnd.Scale = scaleFactor;

        LogHelper.Info(hWnd.ToString());

        _hwndInfo = hWnd;
        return true;
    }

    /// <summary>
    /// 进行钓鱼操作
    /// </summary>
    /// <returns></returns>
    private async Task CastFishing()
    {
        // 模拟按键按下和释放
        User32.keybd_event((byte)FishingKey, 0, InputEventConstant.KeyEventKeyDown, 0);
        await Task.Delay(100);
        User32.keybd_event((byte)FishingKey, 0, InputEventConstant.KeyEventKeyUp, 0);

        // 等待动画结束
        await Task.Delay(2000);
    }

    /// <summary>
    /// 查找鱼漂以及位置
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    private bool FindBobber(RECT rect, out (int, int, int) point)
    {
        // 检测鱼漂位置
        LogHelper.Info("开始检测鱼漂...");
        // 截图窗口区域的图片
        point = FindBobberPoint(rect);
        // 如果点数 >= 300， 则认为鱼漂存在
        return point.Item3 >= 300;
    }


    /// <summary>
    /// 通过多次检测位置变化
    /// </summary>
    /// <returns></returns>
    private async Task<bool> DetectBobberBite(RECT rect, (int, int, int) lastPos, long utc)
    {
        await Task.Yield();
        // 等待鱼上钩，如果没有停止或者超时，一直继续
        while (_isRunning && DateTime.Now.ToFileTimeUtc() < utc )
        {
            var current = FindBobberPoint(rect);
            if (current.Count <= 100)
            {
                LogHelper.Info("未检测到鱼漂，可能是鱼漂消失或位置不正确");
                return true;
            }
            
            var distance = Math.Pow(current.X - lastPos.Item1, 2) + Math.Pow(current.Y - lastPos.Item2, 2);
            if (distance >= 1000)
            {
                return true;
            }
        }
        
        return false;
    }

    private (int X, int Y, int Count) FindBobberPoint(RECT rect)
    {
        using var screenshot = ScreenCapture.CaptureScreenRegion(rect, _hwndInfo.Scale);
        
        
        var x = screenshot.Width / 4;
        var y = screenshot.Height / 5;
        // var bitmap = new Bitmap(x, y);
        // bitmap = screenshot.Clone(new Rectangle(x, y, x * 2, y * 2), screenshot.PixelFormat);
        // bitmap.Save($"screenshot/cut{DateTime.Now:yyyyMMdd_HHmmss}.png");
        // bitmap.Dispose();
        
        // 符合鱼漂颜色的点
        var bobberColoredPoints = FilterImage(screenshot, x, y, x, y);
        if (bobberColoredPoints.Count == 0)
        {
            return (0, 0, 0);
        }
        // 经过中心化筛选出的鱼漂点
        var multipleIteration = MultipleIteration(bobberColoredPoints);
        // 鱼漂中心点
        return CenterOfBobber(multipleIteration);
    }


    /// <summary>
    /// 收杆操作
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rect"></param>
    /// <param name="hWndInfo"></param>
    private async Task ReelIn((int, int, int) position, RECT rect)
    {
        // 由于有DPI缩放，需要计算出鼠标点击位置
        var x = (int)((rect.Left + position.Item1) / _hwndInfo!.Scale);
        var y = (int)((rect.Top + position.Item2) / _hwndInfo.Scale);

        LogHelper.Info($"点击位置: {x}, {y} (原始位置: {position.Item1}, {position.Item2})");

        // 设置鼠标位置
        User32.SetCursorPos(x, y);
        await Task.Delay(100); // 等待鼠标移动

        // 模拟鼠标左键点击
        User32.mouse_event(InputEventConstant.MouseLeftDown, 0, 0, 0, 0);
        await Task.Delay(100); // 等待点击效果
        User32.mouse_event(InputEventConstant.MouseLeftUp, 0, 0, 0, 0);
    }


    
    #endregion

    #region  算法
    
    /// <summary>
    /// 计算图像中符合条件的点
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    private List<(int, int)> FilterImage(Bitmap bitmap, int x, int y, int width, int height, double threshold = 1)
    {
        var points = new List<(int, int)>();
        for (int i = x; i < x + width; i++)
        {
            for (int j = y; j < y + height; j++)
            {
                // 获取像素颜色
                var pixelColor = bitmap.GetPixel(i, j);
                if (pixelColor.R > Math.Max(pixelColor.B, pixelColor.G) * threshold)
                {
                    points.Add((i, j));
                }
            }
        }

        return points;
    }
    
    
    /// <summary>
    /// 计算中心点，以及点的数量
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    private (int X, int Y, int Count) CenterOfBobber(List<(int, int)> list)
    {
        // 计算质心
        double sumX = list.Sum(p => p.Item1);
        double sumY = list.Sum(p => p.Item2);
        var count = list.Count;
        
        LogHelper.Info($"中心点计算: X={sumX / count}, Y={sumY / count}, 点数={count}");

        return ((int)(sumX / count), (int)(sumY / count), count);
    }
    

    private List<(int, int)> Statistical(List<(int, int)> list, double threshold = 1.5)
    {
        // 计算x坐标的中位数和四分位距
        var xValues = list.Select(p => p.Item1).OrderBy(x => x).ToList();
        double xMedian = xValues[xValues.Count / 2];
        double xQ1 = xValues[xValues.Count / 4];
        double xQ3 = xValues[xValues.Count * 3 / 4];
        double xIQR = xQ3 - xQ1;

        // 计算y坐标的中位数和四分位距
        var yValues = list.Select(p => p.Item2).OrderBy(y => y).ToList();
        double yMedian = yValues[yValues.Count / 2];
        double yQ1 = yValues[yValues.Count / 4];
        double yQ3 = yValues[yValues.Count * 3 / 4];
        double yIQR = yQ3 - yQ1;

        // 过滤离群点
        var filteredPoints = list.Where(p =>
            p.Item1 >= xQ1 - threshold * xIQR &&
            p.Item1 <= xQ3 + threshold * xIQR &&
            p.Item2 >= yQ1 - threshold * yIQR &&
            p.Item2 <= yQ3 + threshold * yIQR).ToList();

        return filteredPoints;
    }
    

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="threshold">标准差阈值</param>
    /// <param name="iterations">迭代次数</param>
    /// <returns></returns>
    private List<(int, int)> MultipleIteration(List<(int, int)> list, double threshold = 2.0, int iterations = 1)
    {
        var currentPoints = list;
        for(var index = 0; index < iterations; index++)
        {
            (double meanX, double meanY) = (currentPoints.Average(p => p.Item1), currentPoints.Average(p => p.Item2));
            // 计算每个点到均值的距离
            var distance = currentPoints
                .Select(p => Math.Sqrt(Math.Pow(p.Item1 - meanX, 2) + Math.Pow(p.Item2 - meanY, 2)))
                .ToList();
            
            // 计算标准差
            double stdDev = Math.Sqrt(distance.Average(d => Math.Pow(d - distance.Average(), 2)));

            // 过滤距离过大的点
            currentPoints = currentPoints
                .Where((p, i) => distance[i] < threshold * stdDev)
                .ToList();
        }

        return currentPoints;
    }
    
    #endregion
}