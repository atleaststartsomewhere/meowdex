using Avalonia.Controls;
using Meowdex.Core.Models;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop.Views;

public sealed partial class AddCatDialog : Window
{
    public AddCatDialog()
    {
        InitializeComponent();
    }

    private void OnSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is CatEditorModel model)
        {
            var cat = model.Cat.Clone();
            Close(cat);
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

    private void OnSync(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is CatEditorModel model)
        {
            model.SyncCurrentToBase();
        }
    }
}
