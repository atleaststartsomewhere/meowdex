using Avalonia.Controls;
using Meowdex.Core.Models;
using Meowdex.Core.Services;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop;

public static class AppServices
{
    private static readonly SemaphoreSlim OverlayGate = new(1, 1);

    public static CatRosterService Roster { get; } = new();
    public static BreedingAdvisorService Advisor { get; } = new();
    public static Window? MainWindow { get; set; }
    public static OverlayHostViewModel OverlayHost { get; set; } = new();
    public static DashboardConfig SettingsConfig { get; set; } = new(3, 1, 0);

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

    public static async Task<DashboardConfig?> ShowSettingsAsync(DashboardConfig current)
    {
        return await ShowOverlayAsync<DashboardConfig?>(tcs => new SettingsOverlayViewModel(current, tcs));
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
}
