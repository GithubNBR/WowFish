
namespace OpenCVForm;

using System;
using OpenCvSharp;

public class ImageFinder
{
    public static Point? FindOnScreen(string templateImagePath, double threshold = 0.9)
    {
        // 截取屏幕
        using (var screen = CaptureScreen())
        using (var template = Cv2.ImRead(templateImagePath, ImreadModes.Color))
        {
            if (template.Empty()) return null;

            // 模板匹配
            using (var result = new Mat())
            {
                Cv2.MatchTemplate(screen, template, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

                if (maxVal >= threshold)
                {
                    // 返回匹配中心点
                    return new Point(
                        maxLoc.X + template.Width / 2,
                        maxLoc.Y + template.Height / 2);
                }
            }
        }
        return null;
    }

    private static Mat CaptureScreen()
    {
        var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
        var bitmap = new Bitmap(bounds.Width, bounds.Height);

        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
        }

        return BitmapToMat(bitmap);
    }

    private static Mat BitmapToMat(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(
            rect,
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);

        try
        {
            // 创建Mat并复制数据
            var mat = Mat.FromPixelData(bitmap.Height, bitmap.Width, MatType.CV_8UC3, bitmapData.Scan0);
            return mat.Clone();  // 克隆以确保数据在解锁后仍然有效
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }
}