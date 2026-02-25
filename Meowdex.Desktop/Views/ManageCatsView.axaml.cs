using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Meowdex.Core.Models;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop.Views;

public sealed partial class ManageCatsView : UserControl
{
    public ManageCatsView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        RosterGrid.AddHandler(InputElement.PointerPressedEvent, OnRowPointerPressed, RoutingStrategies.Bubble, true);
    }

    private async void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is ManageCatsViewModel vm)
        {
            await vm.RefreshAsync();
        }
    }

    private void OnBack(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ManageCatsViewModel vm)
        {
            vm.GoToDashboard();
        }
    }

    private async void OnAdd(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ManageCatsViewModel vm)
        {
            return;
        }

        var result = await AppServices.ShowAddCatAsync();
        if (result is not null)
        {
            await vm.AddCatAsync(result);
        }
    }

    private async void OnSettings(object? sender, RoutedEventArgs e)
    {
        var result = await AppServices.ShowSettingsAsync(AppServices.SettingsConfig);
        if (result is not null)
        {
            AppServices.SettingsConfig = result;
        }
    }

    private async void OnLookup(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ManageCatsViewModel vm)
        {
            await vm.LookupAsync();
        }
    }

    private bool _openingEdit;

    private async void OnRowPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (_openingEdit || DataContext is not ManageCatsViewModel vm || sender is not DataGrid grid)
        {
            return;
        }

        if (e.Source is not Control source)
        {
            return;
        }

        var row = source.FindAncestorOfType<DataGridRow>();
        if (row?.DataContext is not CatProfile cat)
        {
            return;
        }

        _openingEdit = true;
        grid.SelectedItem = null;

        var result = await AppServices.ShowEditCatAsync(cat);
        if (result is not null)
        {
            switch (result.Action)
            {
                case EditCatAction.Save when result.Cat is not null:
                    await vm.UpdateCatAsync(result.Cat);
                    break;
                case EditCatAction.Delete:
                    if (result.Cat is not null)
                    {
                        var ok = await AppServices.ConfirmAsync($"Remove cat #{result.Cat.Id}?");
                        if (ok)
                        {
                            await vm.DeleteCatAsync(result.Cat.Id);
                        }
                    }
                    break;
            }
        }

        _openingEdit = false;
    }
}
