using System.Collections.ObjectModel;
using Avalonia.Threading;
using Meowdex.Core.Models;
using Meowdex.Core.Services;

namespace Meowdex.Desktop.ViewModels;

public sealed class ManageCatsViewModel : ViewModelBase
{
    private readonly INavigationService _nav;
    private readonly CatRosterService _roster = AppServices.Roster;

    private int _lookupId;

    public ManageCatsViewModel(INavigationService nav)
    {
        _nav = nav;
        Cats = new ObservableCollection<CatProfile>();
        GenderOptions = Enum.GetValues<CatGender>().ToList();
        SexualityOptions = Enum.GetValues<CatSexuality>().ToList();
    }

    public ObservableCollection<CatProfile> Cats { get; }

    public IReadOnlyList<CatGender> GenderOptions { get; }
    public IReadOnlyList<CatSexuality> SexualityOptions { get; }

    public int LookupId
    {
        get => _lookupId;
        set => SetProperty(ref _lookupId, value);
    }

    public async Task RefreshAsync()
    {
        var cats = await _roster.GetCatsAsync();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Cats.Clear();
            foreach (var cat in cats)
            {
                Cats.Add(cat);
            }
        });
    }

    public async Task AddCatAsync(CatProfile cat)
    {
        await _roster.AddCatAsync(cat);
        await RefreshAsync();
    }

    public async Task LookupAsync()
    {
        if (LookupId <= 0)
        {
            return;
        }

        var cat = await _roster.GetByIdAsync(LookupId);
        if (cat is null)
        {
            return;
        }

        var updated = cat.Clone().WithId(cat.Id);
        await UpdateCatAsync(updated);
    }

    public async Task UpdateCatAsync(CatProfile cat)
    {
        await _roster.UpdateCatAsync(cat);
        await RefreshAsync();
    }

    public async Task DeleteCatAsync(int catId)
    {
        await _roster.DeleteCatAsync(catId);
        await RefreshAsync();
    }

    public void GoToDashboard()
    {
        _nav.NavigateTo(AppPage.Dashboard);
    }
}
