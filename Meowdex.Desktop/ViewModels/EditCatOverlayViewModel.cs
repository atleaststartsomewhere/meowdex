using Meowdex.Core.Models;

namespace Meowdex.Desktop.ViewModels;

public sealed class EditCatOverlayViewModel : OverlayViewModelBase
{
    private readonly TaskCompletionSource<EditCatResult?> _tcs;
    private readonly int _catId;

    public EditCatOverlayViewModel(CatProfile cat, TaskCompletionSource<EditCatResult?> tcs)
    {
        _tcs = tcs;
        _catId = cat.Id;
        Editor = new CatEditorModel(cat.Clone());
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        DeleteCommand = new RelayCommand(Delete);
        SyncCommand = new RelayCommand(() => Editor.SyncCurrentToBase());
    }

    public CatEditorModel Editor { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand SyncCommand { get; }

    private void Save()
    {
        var updated = Editor.Cat.Clone();
        updated = updated.WithId(_catId);
        _tcs.TrySetResult(new EditCatResult(EditCatAction.Save, updated));
        CloseOverlay();
    }

    private void Cancel()
    {
        _tcs.TrySetResult(new EditCatResult(EditCatAction.Cancel, null));
        CloseOverlay();
    }

    private void Delete()
    {
        _tcs.TrySetResult(new EditCatResult(EditCatAction.Delete, Editor.Cat.Clone().WithId(_catId)));
        CloseOverlay();
    }
}

public sealed record EditCatResult(EditCatAction Action, CatProfile? Cat);

public enum EditCatAction
{
    Save,
    Delete,
    Cancel
}
