namespace AutoScript;

public interface IAutoScript
{
    
    public string Name { get; }
    
    public Task StartAsync(CancellationToken cancellationToken = default);
    
    public Task StopAsync();
    
    public bool IsRunning { get; }

    public object ScriptData { get; }
}