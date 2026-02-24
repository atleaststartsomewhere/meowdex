namespace Meowdex.App.Models;

public enum CatGender
{
    Male,
    Female,
    Fluid
}

public enum CatSexuality
{
    GayLesbian,
    Bi,
    Straight
}

public sealed class CatProfile
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public bool IsRetired { get; set; }

    public CatGender Gender { get; set; } = CatGender.Female;
    public CatSexuality Sexuality { get; set; } = CatSexuality.Bi;

    public int StrengthBase { get; set; }
    public int StrengthCurrent { get; set; }
    public int DexterityBase { get; set; }
    public int DexterityCurrent { get; set; }
    public int StaminaBase { get; set; }
    public int StaminaCurrent { get; set; }
    public int IntellectBase { get; set; }
    public int IntellectCurrent { get; set; }
    public int SpeedBase { get; set; }
    public int SpeedCurrent { get; set; }
    public int CharismaBase { get; set; }
    public int CharismaCurrent { get; set; }
    public int LuckBase { get; set; }
    public int LuckCurrent { get; set; }

    public string Notes { get; set; } = string.Empty;

    public double BaseAverage => (StrengthBase + DexterityBase + StaminaBase + IntellectBase + SpeedBase + CharismaBase + LuckBase) / 7.0;
    public double CurrentAverage => (StrengthCurrent + DexterityCurrent + StaminaCurrent + IntellectCurrent + SpeedCurrent + CharismaCurrent + LuckCurrent) / 7.0;

    public int SevenMask =>
        (StrengthBase == 7 ? 1 : 0) |
        (DexterityBase == 7 ? 1 << 1 : 0) |
        (StaminaBase == 7 ? 1 << 2 : 0) |
        (IntellectBase == 7 ? 1 << 3 : 0) |
        (SpeedBase == 7 ? 1 << 4 : 0) |
        (CharismaBase == 7 ? 1 << 5 : 0) |
        (LuckBase == 7 ? 1 << 6 : 0);

    public int BaseSevenCount => CountBits(SevenMask);
    public bool HasNaturalSeven => BaseSevenCount > 0;

    public static int ClampBase(int value) => Math.Clamp(value, 0, 7);
    public static int ClampCurrent(int value) => Math.Clamp(value, 0, 99);

    public CatProfile Clone() => new()
    {
        Id = Id,
        Name = Name,
        IsRetired = IsRetired,
        Gender = Gender,
        Sexuality = Sexuality,
        StrengthBase = StrengthBase,
        StrengthCurrent = StrengthCurrent,
        DexterityBase = DexterityBase,
        DexterityCurrent = DexterityCurrent,
        StaminaBase = StaminaBase,
        StaminaCurrent = StaminaCurrent,
        IntellectBase = IntellectBase,
        IntellectCurrent = IntellectCurrent,
        SpeedBase = SpeedBase,
        SpeedCurrent = SpeedCurrent,
        CharismaBase = CharismaBase,
        CharismaCurrent = CharismaCurrent,
        LuckBase = LuckBase,
        LuckCurrent = LuckCurrent,
        Notes = Notes
    };

    public CatProfile WithId(int id) => new()
    {
        Id = id,
        Name = Name,
        IsRetired = IsRetired,
        Gender = Gender,
        Sexuality = Sexuality,
        StrengthBase = StrengthBase,
        StrengthCurrent = StrengthCurrent,
        DexterityBase = DexterityBase,
        DexterityCurrent = DexterityCurrent,
        StaminaBase = StaminaBase,
        StaminaCurrent = StaminaCurrent,
        IntellectBase = IntellectBase,
        IntellectCurrent = IntellectCurrent,
        SpeedBase = SpeedBase,
        SpeedCurrent = SpeedCurrent,
        CharismaBase = CharismaBase,
        CharismaCurrent = CharismaCurrent,
        LuckBase = LuckBase,
        LuckCurrent = LuckCurrent,
        Notes = Notes
    };

    private static int CountBits(int mask)
    {
        var count = 0;
        var value = mask;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }

        return count;
    }
}
