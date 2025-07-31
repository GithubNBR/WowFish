namespace NAudioForm;

using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class AudioPatternMatcher
{
    private readonly uint _targetProcessId;
    private readonly Dictionary<string, float[]> _audioPatterns;
    private readonly float _matchThreshold;
    private WasapiLoopbackCapture _capture;
    private BufferedWaveProvider _bufferedWaveProvider;
    private readonly int _sampleRate = 44100; // 采样率
    private readonly int _fftLength = 4096; // FFT长度

    public AudioPatternMatcher(uint processId, Dictionary<string, float[]> audioPatterns, float matchThreshold = 0.85f)
    {
        _targetProcessId = processId;
        _audioPatterns = audioPatterns;
        _matchThreshold = matchThreshold;
    }

    public void StartMonitoring()
    {
        var deviceEnumerator = new MMDeviceEnumerator();
        var defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        
        _capture = new WasapiLoopbackCapture(defaultDevice);
        _bufferedWaveProvider = new BufferedWaveProvider(_capture.WaveFormat)
        {
            BufferLength = _sampleRate * 5 * 4, // 5秒缓冲
            DiscardOnBufferOverflow = true
        };

        _capture.DataAvailable += OnAudioDataAvailable;
        _capture.StartRecording();
    }

    private void OnAudioDataAvailable(object sender, WaveInEventArgs e)
    {
        // 检查当前是否是目标进程在播放音频
        var session = GetAudioSession(_targetProcessId);
        if (session?.AudioMeterInformation.MasterPeakValue < 0.01f)
            return;

        _bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);

        // 每2秒分析一次音频
        if (_bufferedWaveProvider.BufferedDuration.TotalSeconds >= 2)
        {
            AnalyzeAudioBuffer();
        }
    }

    private AudioSessionControl? GetAudioSession(uint processId)
    {
        var deviceEnumerator = new MMDeviceEnumerator();
        var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        var sessionCollection = device.AudioSessionManager.Sessions;
        for(var index = 0; index < sessionCollection.Count; index++)
        {
            var session = sessionCollection[index];
            if (session.GetProcessID == processId)
                return session;
        }
        
        return null;
    }

    private void AnalyzeAudioBuffer()
    {
        // 读取缓冲区的音频数据
        byte[] buffer = new byte[_bufferedWaveProvider.BufferedBytes];
        _bufferedWaveProvider.Read(buffer, 0, buffer.Length);

        // 转换为浮点数组用于分析
        float[] audioData = ConvertByteToFloat(buffer);

        // 提取音频特征
        float[] currentFeatures = ExtractAudioFeatures(audioData);

        // 与预定义模式匹配
        foreach (var pattern in _audioPatterns)
        {
            float similarity = CalculateSimilarity(currentFeatures, pattern.Value);
            if (similarity > _matchThreshold)
            {
                OnPatternMatched?.Invoke(this, pattern.Key);
                _bufferedWaveProvider.ClearBuffer(); // 清除缓冲区避免重复触发
                break;
            }
        }
    }

    private float[] ExtractAudioFeatures(float[] audioData)
    {
        // 这里简化为使用RMS能量作为特征，实际应用应该使用更复杂的特征如MFCC
        int windowSize = 1024;
        int featureCount = audioData.Length / windowSize;
        float[] features = new float[featureCount];
        
        for (int i = 0; i < featureCount; i++)
        {
            int start = i * windowSize;
            int end = Math.Min(start + windowSize, audioData.Length);
            features[i] = CalculateRMS(audioData, start, end);
        }
        
        return features;
    }

    private float CalculateRMS(float[] buffer, int start, int end)
    {
        double sum = 0;
        for (int i = start; i < end; i++)
        {
            sum += buffer[i] * buffer[i];
        }
        return (float)Math.Sqrt(sum / (end - start));
    }

    private float CalculateSimilarity(float[] v1, float[] v2)
    {
        // 简单的余弦相似度计算
        float dot = 0, mag1 = 0, mag2 = 0;
        int len = Math.Min(v1.Length, v2.Length);
        
        for (int i = 0; i < len; i++)
        {
            dot += v1[i] * v2[i];
            mag1 += v1[i] * v1[i];
            mag2 += v2[i] * v2[i];
        }
        
        return dot / (float)(Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }

    private float[] ConvertByteToFloat(byte[] buffer)
    {
        float[] floatBuffer = new float[buffer.Length / 4];
        for (int i = 0; i < floatBuffer.Length; i++)
        {
            floatBuffer[i] = BitConverter.ToSingle(buffer, i * 4);
        }
        return floatBuffer;
    }

    public event EventHandler<string> OnPatternMatched;

    public void StopMonitoring()
    {
        _capture?.StopRecording();
        _capture?.Dispose();
    }
}