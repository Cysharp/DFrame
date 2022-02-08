namespace DFrame.Controller;

internal interface INotifyStateChanged
{
    event Action? StateChanged;
}