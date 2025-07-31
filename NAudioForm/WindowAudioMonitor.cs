using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;

namespace NAudioForm;

using System;
using NAudio.CoreAudioApi;

public class WindowAudioMonitor
{
    private readonly IntPtr targetHwnd;
    private readonly uint targetProcessId;
    private MMDevice defaultDevice;
    private WasapiLoopbackCapture capture;
    private bool isTargetPlaying;

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public WindowAudioMonitor(IntPtr hwnd)
    {
        targetHwnd = hwnd;
        GetWindowThreadProcessId(hwnd, out targetProcessId);
    }

    public void StartMonitoring()
    {
        var enumerator = new MMDeviceEnumerator();
        defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        
        capture = new WasapiLoopbackCapture(defaultDevice);
        capture.DataAvailable += OnDataAvailable;
        capture.StartRecording();
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // 获取当前活动的音频会话
        var sessionEnumerator = defaultDevice.AudioSessionManager.Sessions;
        
        for (int i = 0; i < sessionEnumerator.Count; i++)
        {
            var session = sessionEnumerator[i];
            if (session.GetProcessID == targetProcessId && session.State == AudioSessionState.AudioSessionStateActive)
            {
                isTargetPlaying = true;
                OnTargetWindowAudioDetected();
                return;
            }
        }
        
        isTargetPlaying = false;
    }

    public event Action TargetWindowAudioDetected;

    protected virtual void OnTargetWindowAudioDetected()
    {
        TargetWindowAudioDetected?.Invoke();
    }

    public void StopMonitoring()
    {
        capture?.StopRecording();
        capture?.Dispose();
    }
}