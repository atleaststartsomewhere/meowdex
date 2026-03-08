using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Meowdex.Desktop.Converters;
using Meowdex.Desktop.ViewModels;
using Meowdex.Desktop.Views;

namespace Meowdex.Desktop;

public sealed class App : Application
{
    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (XamlLoadException ex) when (ex.Message.Contains("No precompiled XAML found", StringComparison.OrdinalIgnoreCase))
        {
            // Fallback for environments where App.axaml precompilation is unavailable.
            RequestedThemeVariant = ThemeVariant.Dark;

            Styles.Add(new FluentTheme());
            RegisterFallbackResources();

            DataTemplates.Add(new FuncDataTemplate<DashboardViewModel>((_, _) => new DashboardView()));
            DataTemplates.Add(new FuncDataTemplate<ManageCatsViewModel>((_, _) => new ManageCatsView()));
            DataTemplates.Add(new FuncDataTemplate<AddCatOverlayViewModel>((_, _) => new AddCatOverlayView()));
            DataTemplates.Add(new FuncDataTemplate<SettingsOverlayViewModel>((_, _) => new SettingsOverlayView()));
            DataTemplates.Add(new FuncDataTemplate<EditCatOverlayViewModel>((_, _) => new EditCatOverlayView()));
            DataTemplates.Add(new FuncDataTemplate<ConfirmOverlayViewModel>((_, _) => new ConfirmOverlayView()));
        }

        Name = "Meowdex";
    }

    private void RegisterFallbackResources()
    {
        Resources["BoolNot"] = new BoolNotConverter();
        Resources["BoolToOpacity"] = new BoolToOpacityConverter();
        Resources["NullToBool"] = new NullToBoolConverter();
        Resources["MaskConverter"] = new MaskConverter();
        Resources["EqualsConverter"] = new EqualsConverter();
        Resources["EqualityMulti"] = new EqualityMultiConverter();
        Resources["GenderIconConverter"] = new GenderIconConverter();
        Resources["SexualityIconConverter"] = new SexualityIconConverter();
        Resources["GenderTextConverter"] = new GenderTextConverter();
        Resources["SexualityTextConverter"] = new SexualityTextConverter();

        Resources["AppBackground"] = SolidColorBrush.Parse("#2B2D31");
        Resources["SurfaceBackground"] = SolidColorBrush.Parse("#313338");
        Resources["SurfaceAltBackground"] = SolidColorBrush.Parse("#383A40");
        Resources["SurfaceRowAltBackground"] = SolidColorBrush.Parse("#3A3C42");
        Resources["BorderColor"] = SolidColorBrush.Parse("#3F4147");
        Resources["TextPrimary"] = SolidColorBrush.Parse("#E6E8EA");
        Resources["TextMuted"] = SolidColorBrush.Parse("#B5BAC1");
        Resources["Accent"] = SolidColorBrush.Parse("#6EA6FF");
        Resources["AccentStrong"] = SolidColorBrush.Parse("#4C8DFF");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = new MainViewModel();
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            AppServices.MainWindow = mainWindow;
            AppServices.OverlayHost = mainViewModel.OverlayHost;
            desktop.MainWindow = mainWindow;
            _ = AppServices.WarmupUpdateStatusAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
