using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.ComponentModel;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Meowdex.Core.Models;
using Meowdex.Core.Services;
using Meowdex.Desktop.ViewModels;
using System.Diagnostics;

namespace Meowdex.Desktop.Views;

public sealed partial class DashboardView : UserControl
{
    private readonly Dictionary<DataGridColumn, ListSortDirection> _breedingSortDirections = new();
    private bool _openingEdit;

    public DashboardView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        AdventuringGrid.AddHandler(InputElement.PointerPressedEvent, OnGridRowPointerPressed, RoutingStrategies.Bubble, true);
        BreedingPoolGrid.AddHandler(InputElement.PointerPressedEvent, OnGridRowPointerPressed, RoutingStrategies.Bubble, true);
        GeneralPopulationGrid.AddHandler(InputElement.PointerPressedEvent, OnGridRowPointerPressed, RoutingStrategies.Bubble, true);
        FullRosterGrid.AddHandler(InputElement.PointerPressedEvent, OnGridRowPointerPressed, RoutingStrategies.Bubble, true);
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
                StabilizeFullRosterGridLayout();
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"DashboardView settings workflow failed: {ex}");
        }
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
            StabilizeFullRosterGridLayout();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"DashboardView add cat workflow failed: {ex}");
        }
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
            Trace.WriteLine($"DashboardView breeding sort failed: {ex}");
        }
    }

    private async void OnGridRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_openingEdit || DataContext is not DashboardViewModel vm || e.Source is not Control source)
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
                    StabilizeFullRosterGridLayout();
                    break;
                case EditCatAction.Delete when result.Cat is not null:
                    if (await AppServices.ConfirmAsync($"Remove cat #{result.Cat.Id}?"))
                    {
                        await AppServices.Roster.DeleteCatAsync(result.Cat.Id);
                        await vm.RefreshAsync();
                        StabilizeFullRosterGridLayout();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"DashboardView row edit workflow failed: {ex}");
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

    private void StabilizeFullRosterGridLayout()
    {
        Dispatcher.UIThread.Post(() =>
        {
            FullRosterGrid.InvalidateMeasure();
            FullRosterGrid.InvalidateArrange();
            FullRosterGrid.InvalidateVisual();
        }, DispatcherPriority.Background);
    }
}
