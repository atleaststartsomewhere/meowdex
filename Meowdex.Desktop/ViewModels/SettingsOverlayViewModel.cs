using System.Reflection;

namespace Meowdex.Desktop.ViewModels;

public sealed class SettingsOverlayViewModel : OverlayViewModelBase
{
    private readonly TaskCompletionSource<SettingsResult?> _tcs;
    private SettingsSection _activeSection = SettingsSection.Configuration;
    private string _importInput = string.Empty;
    private ImportSummaryData? _importSummary;
    private int _selectedProfile;

    public SettingsOverlayViewModel(DashboardConfig current, int activeProfile, TaskCompletionSource<SettingsResult?> tcs)
    {
        _tcs = tcs;
        Config = new ConfigDialogModel
        {
            TopCatCount = current.TopCatCount,
            BackfillsPerMask = current.BackfillsPerMask,
            MinMaskSevenCount = current.MinMaskSevenCount
        };
        _selectedProfile = activeProfile is >= 1 and <= 3 ? activeProfile : 1;

        ApplyCommand = new RelayCommand(Apply);
        CloseCommand = new RelayCommand(Cancel);
        SetSectionCommand = new RelayCommand(param => SetSection(param?.ToString()));
        RunImportCommand = new AsyncRelayCommand(RunImportAsync, () => !string.IsNullOrWhiteSpace(ImportInput));
    }

    public ConfigDialogModel Config { get; }
    public RelayCommand ApplyCommand { get; }
    public RelayCommand CloseCommand { get; }
    public RelayCommand SetSectionCommand { get; }
    public AsyncRelayCommand RunImportCommand { get; }
    public IReadOnlyList<int> ProfileOptions { get; } = [1, 2, 3];

    public SettingsSection ActiveSection
    {
        get => _activeSection;
        set => SetProperty(ref _activeSection, value);
    }

    public string ImportInput
    {
        get => _importInput;
        set
        {
            if (SetProperty(ref _importInput, value))
            {
                RunImportCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ImportSummaryData? ImportSummary
    {
        get => _importSummary;
        private set => SetProperty(ref _importSummary, value);
    }

    public int SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (!SetProperty(ref _selectedProfile, value))
            {
                return;
            }

            var profileConfig = AppServices.GetProfileConfig(_selectedProfile);
            Config.TopCatCount = profileConfig.TopCatCount;
            Config.BackfillsPerMask = profileConfig.BackfillsPerMask;
            Config.MinMaskSevenCount = profileConfig.MinMaskSevenCount;
        }
    }

    public string BuildIdentity
    {
        get
        {
            var assembly = typeof(App).Assembly;
            var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                return $"Build: {info}";
            }

            var version = assembly.GetName().Version?.ToString() ?? "unknown";
            return $"Build: {version}";
        }
    }

    private void Apply()
    {
        _tcs.TrySetResult(new SettingsResult(
            SelectedProfile,
            new DashboardConfig(Config.TopCatCount, Config.BackfillsPerMask, Config.MinMaskSevenCount)));
        CloseOverlay();
    }

    private void Cancel()
    {
        _tcs.TrySetResult(null);
        CloseOverlay();
    }

    private void SetSection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (Enum.TryParse<SettingsSection>(value, out var section))
        {
            ActiveSection = section;
        }
    }

    private async Task RunImportAsync()
    {
        var lines = ImportInput.Replace("\r", string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var summary = new ImportSummaryData { TotalLines = lines.Length };
        var parsed = new List<Meowdex.Core.Models.CatProfile>();

        for (var i = 0; i < lines.Length; i++)
        {
            if (TryParseLine(lines[i], i + 1, out var cat, out var error))
            {
                summary.ParsedRows++;
                parsed.Add(cat!);
            }
            else
            {
                summary.Errors.Add(error!);
            }
        }

        var existing = await AppServices.Roster.GetCatsAsync();
        var seen = existing.Select(BuildDuplicateKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unique = new List<Meowdex.Core.Models.CatProfile>();

        foreach (var cat in parsed)
        {
            var key = BuildDuplicateKey(cat);
            if (!seen.Add(key))
            {
                summary.SkippedDuplicates++;
                continue;
            }

            unique.Add(cat);
        }

        var imported = await AppServices.Roster.AddCatsAsync(unique);
        summary.Imported = imported.Count;
        ImportSummary = summary;
    }

    private static bool TryParseLine(string line, int lineNumber, out Meowdex.Core.Models.CatProfile? cat, out string? error)
    {
        cat = null;
        error = null;

        var parts = line.Split('\t');
        if (parts.Length != 16)
        {
            error = $"Line {lineNumber}: expected 16 tab-separated columns, got {parts.Length}.";
            return false;
        }

        var name = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            error = $"Line {lineNumber}: name is empty.";
            return false;
        }

        var isRetired = parts[1].Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);

        var numbers = new int[14];
        for (var i = 0; i < 14; i++)
        {
            if (!int.TryParse(parts[i + 2], out numbers[i]))
            {
                error = $"Line {lineNumber}: invalid number '{parts[i + 2]}' at column {i + 3}.";
                return false;
            }
        }

        cat = new Meowdex.Core.Models.CatProfile
        {
            Name = name,
            IsRetired = isRetired,
            Gender = Meowdex.Core.Models.CatGender.Fluid,
            Sexuality = Meowdex.Core.Models.CatSexuality.Bi,
            StrengthCurrent = numbers[0],
            StrengthBase = numbers[1],
            DexterityCurrent = numbers[2],
            DexterityBase = numbers[3],
            StaminaCurrent = numbers[4],
            StaminaBase = numbers[5],
            IntellectCurrent = numbers[6],
            IntellectBase = numbers[7],
            SpeedCurrent = numbers[8],
            SpeedBase = numbers[9],
            CharismaCurrent = numbers[10],
            CharismaBase = numbers[11],
            LuckCurrent = numbers[12],
            LuckBase = numbers[13]
        };

        return true;
    }

    private static string BuildDuplicateKey(Meowdex.Core.Models.CatProfile cat)
    {
        return string.Join("|",
            cat.Name.Trim().ToLowerInvariant(),
            cat.StrengthCurrent,
            cat.StrengthBase,
            cat.DexterityCurrent,
            cat.DexterityBase,
            cat.StaminaCurrent,
            cat.StaminaBase,
            cat.IntellectCurrent,
            cat.IntellectBase,
            cat.SpeedCurrent,
            cat.SpeedBase,
            cat.CharismaCurrent,
            cat.CharismaBase,
            cat.LuckCurrent,
            cat.LuckBase,
            cat.IsRetired);
    }

    public enum SettingsSection
    {
        Configuration,
        ImportData
    }

    public sealed class ImportSummaryData
    {
        public int TotalLines { get; set; }
        public int ParsedRows { get; set; }
        public int Imported { get; set; }
        public int SkippedDuplicates { get; set; }
        public List<string> Errors { get; } = [];
    }
}
