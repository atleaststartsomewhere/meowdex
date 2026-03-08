using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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
        try
        {
            if (DataContext is DashboardViewModel vm)
            {
                await vm.RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"DashboardView attach refresh failed: {ex}");
        }
    }

    private async void OnSettings(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is not DashboardViewModel vm)
            {
                return;
            }

            var result = await AppServices.ShowSettingsAsync(new DashboardConfig(vm.TopCatCount, vm.BackfillsPerMask, vm.MinMaskSevenCount));
            if (result is not null)
            {
                AppServices.ApplySettings(result);
                vm.ApplyConfig(AppServices.SettingsConfig);
                await vm.RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"DashboardView settings workflow failed: {ex}");
        }
    }

    private void OnGoHome(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            vm.SetSectionCommand.Execute("Snapshot");
        }
    }

    private void OnClearTeam(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            vm.ClearCurrentTeam();
        }
    }

    private async void OnRetireTeam(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm)
        {
            return;
        }

        var teamCats = vm.AdventuringTeam
            .Where(slot => slot.Cat is not null && !slot.Cat.IsRetired)
            .Select(slot => slot.Cat!)
            .DistinctBy(cat => cat.Id)
            .ToList();

        if (teamCats.Count == 0)
        {
            return;
        }

        var catList = string.Join(Environment.NewLine, teamCats.Select(cat => $"- #{cat.Id} {cat.Name}"));
        var message =
            "Retire the following adventuring team cats?" +
            Environment.NewLine + Environment.NewLine +
            catList;

        var confirmed = await AppServices.ConfirmAsync(message);
        if (!confirmed)
        {
            return;
        }

        await vm.RetireCurrentTeamAsync();
    }

    private void OnTeamSlotToggle(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm || sender is not ToggleButton toggle || toggle.Tag is null)
        {
            return;
        }

        if (!int.TryParse(toggle.Tag.ToString(), out var oneBasedRow) || oneBasedRow < 1 || oneBasedRow > 4)
        {
            return;
        }

        var targetIndex = oneBasedRow - 1;
        vm.SelectedTeamSlotIndex = vm.SelectedTeamSlotIndex == targetIndex ? -1 : targetIndex;
        e.Handled = true;
    }

    private async void OnAddCat(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is not DashboardViewModel vm)
            {
                return;
            }

            var cat = await AppServices.ShowAddCatAsync();
            if (cat is null)
            {
                return;
            }

            await AppServices.Roster.AddCatAsync(cat);
            await vm.RefreshAsync();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"DashboardView add cat workflow failed: {ex}");
        }
    }
}
