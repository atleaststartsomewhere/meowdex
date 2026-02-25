using Avalonia.Controls;

namespace Meowdex.Desktop.Views;

public sealed partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
        DataContext = new ConfirmDialogModel(string.Empty);
    }

    public ConfirmDialog(string message)
    {
        InitializeComponent();
        DataContext = new ConfirmDialogModel(message);
    }

    private void OnConfirm(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    private sealed record ConfirmDialogModel(string Message);
}
