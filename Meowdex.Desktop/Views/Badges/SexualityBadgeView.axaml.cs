using Avalonia;
using Avalonia.Controls;
using Meowdex.Core.Models;

namespace Meowdex.Desktop.Views.Badges;

public sealed partial class SexualityBadgeView : UserControl
{
    public static readonly StyledProperty<CatSexuality> SexualityProperty =
        AvaloniaProperty.Register<SexualityBadgeView, CatSexuality>(nameof(Sexuality));

    public SexualityBadgeView()
    {
        InitializeComponent();
    }

    public CatSexuality Sexuality
    {
        get => GetValue(SexualityProperty);
        set => SetValue(SexualityProperty, value);
    }

}
