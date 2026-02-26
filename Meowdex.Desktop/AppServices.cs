using Avalonia.Controls;
using Meowdex.Core.Models;
using Meowdex.Core.Services;
using Meowdex.Desktop.Services;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop;

public static class AppServices
{
    private static readonly SemaphoreSlim OverlayGate = new(1, 1);
    private static readonly PlayerProfileStore ProfileStore = new();
    private static PlayerProfileState _profileState;

    public static CatRosterService Roster { get; } = new();
    public static BreedingAdvisorService Advisor { get; } = new();
    public static Window? MainWindow { get; set; }
    public static OverlayHostViewModel OverlayHost { get; set; } = new();
    public static int ActiveProfile { get; private set; }
    public static DashboardConfig SettingsConfig { get; private set; } = new(3, 1, 0);

    static AppServices()
    {
        _profileState = ProfileStore.Load();
        ActiveProfile = NormalizeProfile(_profileState.ActiveProfile);
        SettingsConfig = _profileState.GetConfig(ActiveProfile);
        Roster.SetActiveProfile(ActiveProfile);
    }

    public static async Task<bool> ConfirmAsync(string message)
    {
        if (MainWindow is null)
        {
            return false;
        }

        return await ShowOverlayAsync<bool>(tcs => new ConfirmOverlayViewModel(message, tcs));
    }

    public static async Task<CatProfile?> ShowAddCatAsync()
    {
        return await ShowOverlayAsync<CatProfile?>(tcs => new AddCatOverlayViewModel(tcs));
    }

    public static DashboardConfig GetProfileConfig(int profileId)
    {
        return _profileState.GetConfig(NormalizeProfile(profileId));
    }

    public static void ApplySettings(SettingsResult result)
    {
        var profileId = NormalizeProfile(result.ProfileId);
        _profileState.ActiveProfile = profileId;
        _profileState.SetConfig(profileId, result.Config);
        ProfileStore.Save(_profileState);

        ActiveProfile = profileId;
        SettingsConfig = result.Config;
        Roster.SetActiveProfile(profileId);
    }

    public static async Task<SettingsResult?> ShowSettingsAsync(DashboardConfig current)
    {
        return await ShowOverlayAsync<SettingsResult?>(tcs => new SettingsOverlayViewModel(current, ActiveProfile, tcs));
    }

    public static async Task<EditCatResult?> ShowEditCatAsync(CatProfile cat)
    {
        return await ShowOverlayAsync<EditCatResult?>(tcs => new EditCatOverlayViewModel(cat, tcs));
    }

    private static async Task<T> ShowOverlayAsync<T>(Func<TaskCompletionSource<T>, OverlayViewModelBase> createOverlay)
    {
        await OverlayGate.WaitAsync();

        try
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var overlay = createOverlay(tcs);
            overlay.RequestClose = () => OverlayHost.Close();
            OverlayHost.Show(overlay);
            return await tcs.Task;
        }
        finally
        {
            OverlayGate.Release();
        }
    }

    private static int NormalizeProfile(int profileId) => profileId is >= 1 and <= 3 ? profileId : 1;
}
