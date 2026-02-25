using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Meowdex.Core.Models;

namespace Meowdex.Desktop.Views.Badges;

public sealed partial class SexualityBadgeView : UserControl
{
    public static readonly StyledProperty<CatSexuality> SexualityProperty =
        AvaloniaProperty.Register<SexualityBadgeView, CatSexuality>(nameof(Sexuality));

    public SexualityBadgeView()
    {
        InitializeComponent();
        UpdateBadge(Sexuality);
    }

    public CatSexuality Sexuality
    {
        get => GetValue(SexualityProperty);
        set => SetValue(SexualityProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SexualityProperty && change.NewValue is CatSexuality sexuality)
        {
            UpdateBadge(sexuality);
        }
    }

    private void UpdateBadge(CatSexuality sexuality)
    {
        var (text, bg, fg) = sexuality switch
        {
            CatSexuality.Bi => ("Bi", "#efe8ff", "#4b1b9b"),
            CatSexuality.GayLesbian => ("G/L", "#e9f7ef", "#1a6b39"),
            CatSexuality.Straight => ("St", "#fff4e0", "#8a4a0b"),
            _ => ("?", "#eef1fb", "#30405f")
        };

        Label.Text = text;
        Badge.Background = Brush.Parse(bg);
        Label.Foreground = Brush.Parse(fg);
    }
}
