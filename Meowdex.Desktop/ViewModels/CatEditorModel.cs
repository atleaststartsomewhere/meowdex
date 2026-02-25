using Meowdex.Core.Models;

namespace Meowdex.Desktop.ViewModels;

public sealed class CatEditorModel : ViewModelBase
{
    public CatEditorModel(CatProfile cat)
    {
        Cat = cat;
        GenderOptions = Enum.GetValues<CatGender>().ToList();
        SexualityOptions = Enum.GetValues<CatSexuality>().ToList();
    }

    public CatProfile Cat { get; }

    public IReadOnlyList<CatGender> GenderOptions { get; }
    public IReadOnlyList<CatSexuality> SexualityOptions { get; }

    public void SyncCurrentToBase()
    {
        Cat.StrengthCurrent = Cat.StrengthBase;
        Cat.DexterityCurrent = Cat.DexterityBase;
        Cat.StaminaCurrent = Cat.StaminaBase;
        Cat.IntellectCurrent = Cat.IntellectBase;
        Cat.SpeedCurrent = Cat.SpeedBase;
        Cat.CharismaCurrent = Cat.CharismaBase;
        Cat.LuckCurrent = Cat.LuckBase;
        RaisePropertyChanged(nameof(Cat));
    }
}
