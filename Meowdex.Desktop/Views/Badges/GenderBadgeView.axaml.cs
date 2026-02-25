using Avalonia;
using Avalonia.Controls;
using Meowdex.Core.Models;

namespace Meowdex.Desktop.Views.Badges;

public sealed partial class GenderBadgeView : UserControl
{
    public static readonly StyledProperty<CatGender> GenderProperty =
        AvaloniaProperty.Register<GenderBadgeView, CatGender>(nameof(Gender));

    public GenderBadgeView()
    {
        InitializeComponent();
    }

    public CatGender Gender
    {
        get => GetValue(GenderProperty);
        set => SetValue(GenderProperty, value);
    }

}
