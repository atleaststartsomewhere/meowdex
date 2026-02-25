using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Meowdex.Desktop.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

#if DEBUG
        DebugSelectionHud.IsVisible = true;
        AddHandler(InputElement.GotFocusEvent, OnAnyControlGotFocus, RoutingStrategies.Bubble);
#endif
    }

#if DEBUG
    private void OnAnyControlGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (e.Source is not Control control)
        {
            if (e.Source is not null)
            {
                DebugSelectionText.Text = $"Selected: {e.Source.GetType().Name}";
            }

            return;
        }

        var id = ResolveIdentifier(control);
        DebugSelectionText.Text = $"Selected: {id}";
    }

    private static string ResolveIdentifier(Control control)
    {
        var name = control.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            return $"{name} ({control.GetType().Name})";
        }

        if (control.Classes.Count > 0)
        {
            return $"{control.GetType().Name} .{string.Join('.', control.Classes)}";
        }

        return control.GetType().Name;
    }
#endif
}
