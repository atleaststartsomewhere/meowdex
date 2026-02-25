using Avalonia.Controls;
using Avalonia.Interactivity;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop.Views;

public sealed partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            await vm.RefreshAsync();
        }
    }

    private void OnManageCats(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            vm.GoToManageCats();
        }
    }

    private async void OnSettings(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm)
        {
            return;
        }

        var result = await AppServices.ShowSettingsAsync(new DashboardConfig(vm.TopCatCount, vm.BackfillsPerMask, vm.MinMaskSevenCount));
        if (result is not null)
        {
            AppServices.SettingsConfig = result;
            vm.ApplyConfig(result);
            await vm.RefreshAsync();
        }
    }
}
