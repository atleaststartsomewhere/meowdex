namespace Meowdex.Desktop.ViewModels;

public sealed class ConfigDialogModel : ViewModelBase
{
    private int _topCatCount;
    private int _backfillsPerMask;
    private int _minMaskSevenCount;

    public int TopCatCount
    {
        get => _topCatCount;
        set => SetProperty(ref _topCatCount, value);
    }

    public int BackfillsPerMask
    {
        get => _backfillsPerMask;
        set => SetProperty(ref _backfillsPerMask, value);
    }

    public int MinMaskSevenCount
    {
        get => _minMaskSevenCount;
        set => SetProperty(ref _minMaskSevenCount, value);
    }
}
