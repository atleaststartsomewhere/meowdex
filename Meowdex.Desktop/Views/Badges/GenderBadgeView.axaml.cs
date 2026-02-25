using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Meowdex.Core.Models;

namespace Meowdex.Desktop.Views.Badges;

public sealed partial class GenderBadgeView : UserControl
{
    public static readonly StyledProperty<CatGender> GenderProperty =
        AvaloniaProperty.Register<GenderBadgeView, CatGender>(nameof(Gender));

    public GenderBadgeView()
    {
        InitializeComponent();
        UpdateBadge(Gender);
    }

    public CatGender Gender
    {
        get => GetValue(GenderProperty);
        set => SetValue(GenderProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == GenderProperty && change.NewValue is CatGender gender)
        {
            UpdateBadge(gender);
        }
    }

    private void UpdateBadge(CatGender gender)
    {
        var (text, bg, fg) = gender switch
        {
            CatGender.Male => ("M", "#e2f4ff", "#0b4a6f"),
            CatGender.Female => ("F", "#ffe6ef", "#9b1b4c"),
            CatGender.Fluid => ("Fl", "#f5e9ff", "#5a1b9b"),
            _ => ("?", "#eef1fb", "#30405f")
        };

        Label.Text = text;
        Badge.Background = Brush.Parse(bg);
        Label.Foreground = Brush.Parse(fg);
    }
}
