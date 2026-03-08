using Avalonia.Controls;
using Avalonia.Interactivity;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop.Views.Sections;

public sealed partial class AdventuringSectionView : UserControl
{
    public AdventuringSectionView()
    {
        InitializeComponent();
    }

    private void OnClearTeam(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            vm.ClearCurrentTeam();
        }
    }

    private void OnResetTeamAuto(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            vm.ResetTeamToFullAuto();
        }
    }

    private async void OnRetireTeam(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm)
        {
            return;
        }

        var teamCats = vm.AdventuringTeam
            .Where(slot => slot.Cat is not null && !slot.Cat.IsRetired)
            .Select(slot => slot.Cat!)
            .DistinctBy(cat => cat.Id)
            .ToList();

        if (teamCats.Count == 0)
        {
            return;
        }

        var catList = string.Join(Environment.NewLine, teamCats.Select(cat => $"- #{cat.Id} {cat.Name}"));
        var message =
            "Retire the following adventuring team cats?" +
            Environment.NewLine + Environment.NewLine +
            catList;

        var confirmed = await AppServices.ConfirmAsync(message);
        if (!confirmed)
        {
            return;
        }

        await vm.RetireCurrentTeamAsync();
    }
}
