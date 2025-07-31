namespace OpenCVForm;

using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

public class FeatureMatcher
{
    /// <summary>
    /// 使用特征匹配查找图片中的目标
    /// </summary>
    public static List<Point2f>? FindByFeatures(string sourceImagePath, string templateImagePath, int minMatches = 10)
    {
        using var source = Cv2.ImRead(sourceImagePath, ImreadModes.Grayscale);
        using var template = Cv2.ImRead(templateImagePath, ImreadModes.Grayscale);
        
        if (source.Empty() || template.Empty())
        {
            Console.WriteLine("无法加载图像");
            return null;
        }

        // 初始化ORB特征检测器
        using var orb = ORB.Create(1000);
        
        // 检测关键点和计算描述符
        KeyPoint[] keypoints1, keypoints2;
        Mat descriptors1 = new Mat(), descriptors2 = new Mat();
        orb.DetectAndCompute(source, null, out keypoints1, descriptors1);
        orb.DetectAndCompute(template, null, out keypoints2, descriptors2);

        // 使用BFMatcher进行匹配
        var bfMatcher = new BFMatcher(NormTypes.Hamming, crossCheck: true);
        var matches = bfMatcher.Match(descriptors1, descriptors2);

        // 筛选最佳匹配
        var goodMatches = matches.OrderBy(x => x.Distance).Take(minMatches).ToList();

        if (goodMatches.Count >= minMatches)
        {
            // 获取匹配点的位置
            var sourcePoints = goodMatches.Select(m => keypoints1[m.QueryIdx].Pt).ToList();
            var templatePoints = goodMatches.Select(m => keypoints2[m.TrainIdx].Pt).ToList();

            // 计算变换矩阵
            Mat homography = Cv2.FindHomography(
                InputArray.Create(templatePoints),
                InputArray.Create(sourcePoints),
                HomographyMethods.Ransac, 5.0);

            // 获取模板图像的角点
            var templateCorners = new[]
            {
                new Point2f(0, 0),
                new Point2f(template.Cols, 0),
                new Point2f(template.Cols, template.Rows),
                new Point2f(0, template.Rows)
            };

            // 变换到源图像中的位置
            var sourceCorners = Cv2.PerspectiveTransform(templateCorners, homography);

            return sourceCorners.ToList();
        }
        
        return null;
    }

    /// <summary>
    /// 绘制特征匹配结果
    /// </summary>
    public static void DrawFeatureMatchResult(string sourceImagePath, string outputImagePath, List<Point2f> corners)
    {
        using (Mat image = Cv2.ImRead(sourceImagePath, ImreadModes.Color))
        {
            // 绘制多边形标记匹配区域
            for (int i = 0; i < corners.Count; i++)
            {
                Cv2.Line(image, (Point)corners[i], (Point)corners[(i + 1) % corners.Count], Scalar.Red, 2);
            }
            Cv2.ImWrite(outputImagePath, image);
        }
    }
}