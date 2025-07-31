namespace NAudioForm;

using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class AsyncAudioPatternMatcher : IDisposable
{
    private readonly uint _targetProcessId;
    private readonly Dictionary<string, float[]> _audioPatterns;
    private readonly float _matchThreshold;
    private WasapiLoopbackCapture _capture;
    private BufferedWaveProvider _bufferedWaveProvider;
    private readonly int _sampleRate = 44100;
    private readonly int _fftLength = 4096;
    private CancellationTokenSource _cts;
    private TaskCompletionSource<bool> _matchCompletionSource;

    public AsyncAudioPatternMatcher(uint processId, Dictionary<string, float[]> audioPatterns, float matchThreshold = 0.85f)
    {
        _targetProcessId = processId;
        _audioPatterns = audioPatterns;
        _matchThreshold = matchThreshold;
    }

    /// <summary>
    /// 开始监听音频并等待匹配（30秒超时）
    /// </summary>
    /// <returns>返回匹配到的模式名称，超时返回null</returns>
    public async Task<string> StartMonitoringWithTimeoutAsync()
    {
        _cts = new CancellationTokenSource();
        _matchCompletionSource = new TaskCompletionSource<bool>();
        
        var deviceEnumerator = new MMDeviceEnumerator();
        var defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        
        _capture = new WasapiLoopbackCapture(defaultDevice);
        _bufferedWaveProvider = new BufferedWaveProvider(_capture.WaveFormat)
        {
            BufferLength = _sampleRate * 5 * 4,
            DiscardOnBufferOverflow = true
        };

        _capture.DataAvailable += OnAudioDataAvailable;
        _capture.StartRecording();

        // 设置30秒超时
        var timeoutTask = Task.Delay(30000, _cts.Token);
        var matchTask = WaitForPatternMatchAsync();
        
        var completedTask = await Task.WhenAny(matchTask, timeoutTask);
        
        // 停止捕获
        StopMonitoring();

        if (completedTask == matchTask)
        {
            return await matchTask;
        }
        
        return null; // 超时返回null
    }

    private async Task<string> WaitForPatternMatchAsync()
    {
        await _matchCompletionSource.Task;
        return _matchedPatternName;
    }

    private string _matchedPatternName;

    private void OnAudioDataAvailable(object sender, WaveInEventArgs e)
    {
        if (_cts.IsCancellationRequested)
            return;

        var session = GetAudioSession(_targetProcessId);
        if (session?.AudioMeterInformation.MasterPeakValue < 0.01f)
            return;

        _bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);

        if (_bufferedWaveProvider.BufferedDuration.TotalSeconds >= 2)
        {
            AnalyzeAudioBuffer();
        }
    }

    private void AnalyzeAudioBuffer()
    {
        byte[] buffer = new byte[_bufferedWaveProvider.BufferedBytes];
        _bufferedWaveProvider.Read(buffer, 0, buffer.Length);
        float[] audioData = ConvertByteToFloat(buffer);
        float[] currentFeatures = ExtractAudioFeatures(audioData);

        foreach (var pattern in _audioPatterns)
        {
            float similarity = CalculateSimilarity(currentFeatures, pattern.Value);
            if (similarity > _matchThreshold)
            {
                _matchedPatternName = pattern.Key;
                _matchCompletionSource?.TrySetResult(true);
                _bufferedWaveProvider.ClearBuffer();
                break;
            }
        }
    }

    public void StopMonitoring()
    {
        _cts?.Cancel();
        _capture?.StopRecording();
        _capture?.Dispose();
        _capture = null;
    }

    public void Dispose()
    {
        StopMonitoring();
        _cts?.Dispose();
    }

    #region 原有的辅助方法保持不变
    private AudioSessionControl GetAudioSession(uint processId)
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

    private float[] ExtractAudioFeatures(float[] audioData)
    {
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
    #endregion
}