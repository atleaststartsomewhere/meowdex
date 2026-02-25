using Avalonia.Controls;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop.Views;

public sealed partial class ConfigDialog : Window
{
    public ConfigDialog()
    {
        InitializeComponent();
    }

    private void OnSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ConfigDialogModel model)
        {
            Close(new DashboardConfig(model.TopCatCount, model.BackfillsPerMask, model.MinMaskSevenCount));
        }
        else
        {
            Close(null);
        }
    }

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }
}
