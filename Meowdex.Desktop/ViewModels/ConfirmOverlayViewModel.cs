namespace Meowdex.Desktop.ViewModels;

public sealed class ConfirmOverlayViewModel : OverlayViewModelBase
{
    private readonly TaskCompletionSource<bool> _tcs;

    public ConfirmOverlayViewModel(string message, TaskCompletionSource<bool> tcs)
    {
        _tcs = tcs;
        Message = message;
        ConfirmCommand = new RelayCommand(Confirm);
        CancelCommand = new RelayCommand(Cancel);
    }

    public string Message { get; }
    public RelayCommand ConfirmCommand { get; }
    public RelayCommand CancelCommand { get; }

    private void Confirm()
    {
        _tcs.TrySetResult(true);
        CloseOverlay();
    }

    private void Cancel()
    {
        _tcs.TrySetResult(false);
        CloseOverlay();
    }
}
