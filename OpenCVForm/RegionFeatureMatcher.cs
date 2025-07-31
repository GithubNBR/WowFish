namespace OpenCVForm;

using OpenCvSharp;
using OpenCvSharp.Features2D;
using System;
using System.Collections.Generic;
using System.Linq;

public class RegionFeatureMatcher
{
    /// <summary>
    /// 在指定区域内比较两张图片的特征相似度
    /// </summary>
    /// <param name="img1">第一张图片</param>
    /// <param name="img2">第二张图片</param>
    /// <param name="roi">感兴趣区域(Rectangle)</param>
    /// <param name="minMatches">最小匹配点数</param>
    /// <returns>匹配分数(0-1)</returns>
    public static double CompareImagesInRegion(Mat img1, Mat img2, Rect roi, int minMatches = 10)
    {
        // 转换为灰度图
        using (Mat gray1 = new Mat(), gray2 = new Mat())
        {
            Cv2.CvtColor(img1, gray1, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(img2, gray2, ColorConversionCodes.BGR2GRAY);

            // 提取ROI区域
            using (Mat roi1 = gray1[roi], roi2 = gray2[roi])
            {
                return MatchFeatures(roi1, roi2, minMatches);
            }
        }
    }

    private static double MatchFeatures(Mat img1, Mat img2, int minMatches)
    {
        // 初始化ORB特征检测器
        using (var orb = ORB.Create(1000))
        {
            // 检测关键点和计算描述符
            KeyPoint[] keypoints1, keypoints2;
            Mat descriptors1 = new Mat(), descriptors2 = new Mat();
            orb.DetectAndCompute(img1, null, out keypoints1, descriptors1);
            orb.DetectAndCompute(img2, null, out keypoints2, descriptors2);

            if (keypoints1.Length < minMatches || keypoints2.Length < minMatches)
                return 0;

            // 使用BFMatcher进行匹配
            using (var bfMatcher = new BFMatcher(NormTypes.Hamming, crossCheck: true))
            {
                var matches = bfMatcher.Match(descriptors1, descriptors2);

                // 筛选最佳匹配
                var goodMatches = matches
                    .OrderBy(x => x.Distance)
                    .Take(Math.Min(minMatches * 2, matches.Length))
                    .ToArray();

                if (goodMatches.Length >= minMatches)
                {
                    // 计算匹配分数 (基于匹配距离)
                    double maxDistance = goodMatches.Max(m => m.Distance);
                    double score = goodMatches.Average(m => 1 - m.Distance / maxDistance);
                    return Math.Round(score, 2);
                }
            }
        }
        return 0;
    }
}