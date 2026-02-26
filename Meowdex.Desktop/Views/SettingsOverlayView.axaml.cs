using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop.Views;

public sealed partial class SettingsOverlayView : UserControl
{
    public SettingsOverlayView()
    {
        InitializeComponent();
        AddHandler(InputElement.KeyDownEvent, OnAnyControlKeyDown, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
    }

    private void OnAnyControlKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SettingsOverlayViewModel vm)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            vm.CloseCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            vm.ApplyCommand.Execute(null);
            e.Handled = true;
        }
    }
}
