namespace Meowdex.Desktop.ViewModels;

public sealed class OverlayHostViewModel : ViewModelBase
{
    private OverlayViewModelBase? _current;

    public OverlayViewModelBase? Current
    {
        get => _current;
        private set
        {
            if (SetProperty(ref _current, value))
            {
                RaisePropertyChanged(nameof(IsOpen));
            }
        }
    }

    public bool IsOpen => Current is not null;

    public void Show(OverlayViewModelBase overlay)
    {
        Current = overlay;
    }

    public void Close()
    {
        Current = null;
    }
}

public abstract class OverlayViewModelBase : ViewModelBase
{
    public Action? RequestClose { get; set; }

    protected void CloseOverlay()
    {
        RequestClose?.Invoke();
    }
}
