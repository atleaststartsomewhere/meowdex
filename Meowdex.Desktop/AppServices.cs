using Avalonia.Controls;
using Meowdex.Core.Models;
using Meowdex.Core.Services;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop;

public static class AppServices
{
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

        var tcs = new TaskCompletionSource<bool>();
        var overlay = new ConfirmOverlayViewModel(message, tcs);
        overlay.RequestClose = () => OverlayHost.Close();
        OverlayHost.Show(overlay);
        return await tcs.Task;
    }

    public static async Task<CatProfile?> ShowAddCatAsync()
    {
        var tcs = new TaskCompletionSource<CatProfile?>();
        var overlay = new AddCatOverlayViewModel(tcs);
        overlay.RequestClose = () => OverlayHost.Close();
        OverlayHost.Show(overlay);
        return await tcs.Task;
    }

    public static async Task<DashboardConfig?> ShowSettingsAsync(DashboardConfig current)
    {
        var tcs = new TaskCompletionSource<DashboardConfig?>();
        var overlay = new SettingsOverlayViewModel(current, tcs);
        overlay.RequestClose = () => OverlayHost.Close();
        OverlayHost.Show(overlay);
        return await tcs.Task;
    }

    public static async Task<EditCatResult?> ShowEditCatAsync(CatProfile cat)
    {
        var tcs = new TaskCompletionSource<EditCatResult?>();
        var overlay = new EditCatOverlayViewModel(cat, tcs);
        overlay.RequestClose = () => OverlayHost.Close();
        OverlayHost.Show(overlay);
        return await tcs.Task;
    }
}
