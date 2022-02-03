namespace DFrame.Controller;

public interface INotifyStateChanged
{
    event Action? StateChanged;
}