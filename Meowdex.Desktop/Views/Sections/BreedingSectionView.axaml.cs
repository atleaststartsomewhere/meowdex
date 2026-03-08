using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Meowdex.Core.Models;
using Meowdex.Core.Services;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop.Views.Sections;

public sealed partial class BreedingSectionView : UserControl
{
    private readonly Dictionary<DataGridColumn, ListSortDirection> _breedingSortDirections = new();
    private bool _openingEdit;
    private CancellationTokenSource? _teamToastCts;

    public BreedingSectionView()
    {
        InitializeComponent();
        BreedingPoolGrid.AddHandler(InputElement.PointerPressedEvent, OnGridRowPointerPressed, RoutingStrategies.Bubble, true);
    }

    private void OnBreedingHelpOpen(object? sender, RoutedEventArgs e)
    {
        BreedingHelpLightbox.IsVisible = true;
        BreedingHelpLightbox.Focus();
    }

    private void OnBreedingHelpClose(object? sender, PointerPressedEventArgs e)
    {
        BreedingHelpLightbox.IsVisible = false;
        e.Handled = true;
    }

    private void OnBreedingHelpKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        BreedingHelpLightbox.IsVisible = false;
        e.Handled = true;
    }

    private void OnRemoveFromBreedingPool(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm && sender is Button button && button.Tag is int catId)
        {
            vm.RemoveFromBreedingPool(catId);
            e.Handled = true;
        }
    }

    private void OnSendToTeam(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm || sender is not Button button || button.Tag is not int catId)
        {
            return;
        }

        if (vm.SelectedTeamSlotIndex < 0)
        {
            ShowTeamToast("Select an adventuring slot first.");
            e.Handled = true;
            return;
        }

        int? preferredRow = vm.SelectedTeamSlotIndex + 1;
        var result = vm.SendToAdventuringTeam(catId, preferredRow);
        if (result == DashboardViewModel.SendToTeamResult.TeamFullNeedsSlotSelection)
        {
            ShowTeamToast("Team is full. Select a slot in the top bar, or manage slots on the Adventuring page.");
        }
        else if (result == DashboardViewModel.SendToTeamResult.AlreadyOnTeam)
        {
            ShowTeamToast("That cat is already on your team.");
        }

        e.Handled = true;
    }

    private void OnBreedingPoolSorting(object? sender, DataGridColumnEventArgs e)
    {
        try
        {
            var sortPath = e.Column.SortMemberPath;
            if (string.IsNullOrWhiteSpace(sortPath) || DataContext is not DashboardViewModel vm)
            {
                return;
            }

            _breedingSortDirections.TryGetValue(e.Column, out var previousDirection);
            var nextDirection = previousDirection == ListSortDirection.Descending
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;

            _breedingSortDirections.Clear();
            _breedingSortDirections[e.Column] = nextDirection;
            vm.SortBreedingPool(sortPath, nextDirection);
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"BreedingSectionView breeding sort failed: {ex}");
        }
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
            Trace.WriteLine($"BreedingSectionView row edit workflow failed: {ex}");
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

    private void ShowTeamToast(string message)
    {
        _teamToastCts?.Cancel();
        _teamToastCts?.Dispose();
        _teamToastCts = new CancellationTokenSource();
        var token = _teamToastCts.Token;

        TeamToastText.Text = message;
        TeamToast.IsVisible = true;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2400, token);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TeamToast.IsVisible = false;
                });
            }
            catch (TaskCanceledException)
            {
                // Replaced by newer toast.
            }
        }, token);
    }
}
