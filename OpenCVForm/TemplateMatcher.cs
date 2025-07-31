namespace OpenCVForm;

using OpenCvSharp;
using System;

public class TemplateMatcher
{
    /// <summary>
    /// 使用模板匹配查找图片中的目标
    /// </summary>
    /// <param name="sourceImagePath">源图像路径</param>
    /// <param name="templateImagePath">模板图像路径</param>
    /// <param name="threshold">匹配阈值(0-1)</param>
    /// <returns>匹配结果的矩形位置</returns>
    public static Rect? FindTemplate(string sourceImagePath, string templateImagePath, double threshold = 0.8)
    {
        using (Mat source = Cv2.ImRead(sourceImagePath, ImreadModes.Color))
        using (Mat template = Cv2.ImRead(templateImagePath, ImreadModes.Color))
        {
            // 如果图像加载失败
            if (source.Empty() || template.Empty())
            {
                Console.WriteLine("无法加载图像");
                return null;
            }

            // 创建结果矩阵
            Mat result = new Mat();
            Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);

            // 查找最佳匹配位置
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

            // 如果匹配度高于阈值
            if (maxVal >= threshold)
            {
                return new Rect(maxLoc, template.Size());
            }
        }

        return null;
    }

    /// <summary>
    /// 在图像上绘制匹配结果的矩形
    /// </summary>
    public static void DrawMatchResult(string sourceImagePath, string outputImagePath, Rect matchRect)
    {
        using (Mat image = Cv2.ImRead(sourceImagePath, ImreadModes.Color))
        {
            // 绘制红色矩形标记匹配区域
            Cv2.Rectangle(image, matchRect, Scalar.Red, 2);
            Cv2.ImWrite(outputImagePath, image);
        }
    }
}