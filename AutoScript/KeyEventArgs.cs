namespace AutoScript;

public class KeyEventArgs(int keyCode) : EventArgs
{
    public int KeyCode { get; } = keyCode;
}