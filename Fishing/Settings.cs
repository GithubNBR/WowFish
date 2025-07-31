namespace Fishing;

public class Settings
{
    public int StartKey { get; set; } = 0x7A; // F11
    public int StopKey { get; set; } = 0x7B; // F12
    
    private Settings()
    {
        
    }
    
    
    private static Settings? _instance;
    
    public static Settings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Settings();
            }
            return _instance;
        }
    }
}