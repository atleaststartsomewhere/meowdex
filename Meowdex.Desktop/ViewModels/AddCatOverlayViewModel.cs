using Meowdex.Core.Models;

namespace Meowdex.Desktop.ViewModels;

public sealed class AddCatOverlayViewModel : OverlayViewModelBase
{
    private readonly TaskCompletionSource<CatProfile?> _tcs;

    public AddCatOverlayViewModel(TaskCompletionSource<CatProfile?> tcs)
    {
        _tcs = tcs;
        var draft = new CatProfile
        {
            Gender = CatGender.Female,
            Sexuality = CatSexuality.Bi
        };
        Editor = new CatEditorModel(draft);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        SyncCommand = new RelayCommand(() => Editor.SyncCurrentToBase());
    }

    public CatEditorModel Editor { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SyncCommand { get; }

    private void Save()
    {
        _tcs.TrySetResult(Editor.Cat.Clone());
        CloseOverlay();
    }

    private void Cancel()
    {
        _tcs.TrySetResult(null);
        CloseOverlay();
    }
}
