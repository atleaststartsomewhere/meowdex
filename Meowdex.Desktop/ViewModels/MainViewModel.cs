namespace Meowdex.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase, INavigationService
{
    private ViewModelBase _currentViewModel;
    private OverlayHostViewModel _overlayHost = new();

    public MainViewModel()
    {
        Dashboard = new DashboardViewModel(this);
        ManageCats = new ManageCatsViewModel(this);
        _currentViewModel = Dashboard;
    }

    public DashboardViewModel Dashboard { get; }
    public ManageCatsViewModel ManageCats { get; }

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public OverlayHostViewModel OverlayHost
    {
        get => _overlayHost;
        set => SetProperty(ref _overlayHost, value);
    }

    public void NavigateTo(AppPage page)
    {
        CurrentViewModel = page switch
        {
            AppPage.Dashboard => Dashboard,
            AppPage.ManageCats => ManageCats,
            _ => Dashboard
        };
    }
}

public interface INavigationService
{
    void NavigateTo(AppPage page);
}

public enum AppPage
{
    Dashboard,
    ManageCats
}
