using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Meowdex.Core.Models;
using Meowdex.Core.Services;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop.Views.Sections;

public sealed partial class AdventuringSectionView : UserControl
{
    private bool _openingEdit;

    public AdventuringSectionView()
    {
        InitializeComponent();
        AdventuringGrid.AddHandler(InputElement.PointerPressedEvent, OnGridRowPointerPressed, RoutingStrategies.Bubble, true);
    }

    private void OnClearTeam(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            vm.ClearCurrentTeam();
        }
    }

    private void OnResetTeamAuto(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            vm.ResetTeamToFullAuto();
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

    private async void OnGridRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_openingEdit || DataContext is not DashboardViewModel vm || e.Source is not Control source)
        {
            return;
        }

        if (source.FindAncestorOfType<Button>() is not null ||
            source.FindAncestorOfType<ToggleButton>() is not null ||
            source.FindAncestorOfType<Expander>() is not null ||
            source.FindAncestorOfType<ComboBox>() is not null)
        {
            return;
        }

        var row = source.FindAncestorOfType<DataGridRow>();
        if (row?.DataContext is null)
        {
            return;
        }

        var cat = TryGetCatFromRow(row.DataContext);
        if (cat is null)
        {
            return;
        }

        _openingEdit = true;
        if (sender is DataGrid grid)
        {
            grid.SelectedItem = null;
        }

        try
        {
            var result = await AppServices.ShowEditCatAsync(cat);
            if (result is null)
            {
                return;
            }

            switch (result.Action)
            {
                case EditCatAction.Save when result.Cat is not null:
                    await AppServices.Roster.UpdateCatAsync(result.Cat);
                    await vm.RefreshAsync();
                    break;
                case EditCatAction.Delete when result.Cat is not null:
                    if (await AppServices.ConfirmAsync($"Remove cat #{result.Cat.Id}?"))
                    {
                        await AppServices.Roster.DeleteCatAsync(result.Cat.Id);
                        await vm.RefreshAsync();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"AdventuringSectionView row edit workflow failed: {ex}");
        }
        finally
        {
            _openingEdit = false;
        }
    }

    private static CatProfile? TryGetCatFromRow(object rowData)
    {
        return rowData switch
        {
            CatProfile cat => cat,
            BreedingPoolEntry entry => entry.Cat,
            GeneralPopulationEntry entry => entry.Cat,
            DashboardViewModel.AdventuringSlot slot => slot.Cat,
            _ => null
        };
    }
}
