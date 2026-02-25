using System.Collections.ObjectModel;
using System.ComponentModel;
using Meowdex.Core.Models;
using Meowdex.Core.Services;

namespace Meowdex.Desktop.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly INavigationService _nav;
    private readonly CatRosterService _roster = AppServices.Roster;
    private readonly BreedingAdvisorService _advisor = AppServices.Advisor;

    private int _topCatCount = 3;
    private int _backfillsPerMask = 1;
    private int _minMaskSevenCount;
    private bool _teamAutoEnabled = true;
    private BreedingPlanResult _plan = new(3, 1, [], [], [], [], 0);
    private DashboardSection _activeSection = DashboardSection.Snapshot;

    public DashboardViewModel(INavigationService nav)
    {
        _nav = nav;
        RowPriorities = new ObservableCollection<RowPrioritySetting>
        {
            new(1, RowStatChoice.None, OnRowPriorityChanged),
            new(2, RowStatChoice.None, OnRowPriorityChanged),
            new(3, RowStatChoice.None, OnRowPriorityChanged),
            new(4, RowStatChoice.None, OnRowPriorityChanged)
        };
        AdventuringTeam = new ObservableCollection<AdventuringSlot>();
        Cats = new ObservableCollection<CatProfile>();
        SetSectionCommand = new RelayCommand(param => SetSection(param?.ToString()));
    }

    public ObservableCollection<CatProfile> Cats { get; }

    public int TotalCats => Cats.Count;
    public int RetiredCount => Cats.Count(cat => cat.IsRetired);
    public int NaturalSevenCount => Cats.Count(cat => cat.HasNaturalSeven);
    public int CoveredSevenCount => CountBits(Plan.CoveredMask);
    public double CoveragePercent => CoveredSevenCount / 7.0 * 100.0;
    public string CoverageSummary => $"{CoveredSevenCount}/7 stats covered";
    public string MissingStatSummary
    {
        get
        {
            var names = GetMissingStatNames(Plan.CoveredMask);
            return names.Count == 0 ? "No missing natural-7 stats." : $"Missing: {string.Join(", ", names)}";
        }
    }
    public int LowPartnerRiskCount => Plan.BreedingPool.Count(entry => entry.CompatiblePartners <= 1);
    public string RiskSummary =>
        LowPartnerRiskCount == 0 ? "No low-partner breeders in pool." : $"{LowPartnerRiskCount} pool cats have <= 1 compatible partner.";
    public string NextActionSummary
    {
        get
        {
            var missing = GetMissingStatNames(Plan.CoveredMask);
            if (missing.Count > 0)
            {
                return $"Find candidates with natural 7 in: {string.Join(", ", missing)}.";
            }

            if (LowPartnerRiskCount > 0)
            {
                return "Review low-partner breeders and consider replacements from Full Roster.";
            }

            return "Coverage is stable. Validate averages and tune backfill settings if needed.";
        }
    }

    public BreedingPlanResult Plan
    {
        get => _plan;
        private set
        {
            if (SetProperty(ref _plan, value))
            {
                RaisePropertyChanged(nameof(CoveredSevenCount));
                RaisePropertyChanged(nameof(CoveragePercent));
                RaisePropertyChanged(nameof(CoverageSummary));
                RaisePropertyChanged(nameof(MissingStatSummary));
                RaisePropertyChanged(nameof(LowPartnerRiskCount));
                RaisePropertyChanged(nameof(RiskSummary));
                RaisePropertyChanged(nameof(NextActionSummary));
            }
        }
    }

    public int TopCatCount
    {
        get => _topCatCount;
        set => SetProperty(ref _topCatCount, value);
    }

    public int BackfillsPerMask
    {
        get => _backfillsPerMask;
        set => SetProperty(ref _backfillsPerMask, value);
    }

    public int MinMaskSevenCount
    {
        get => _minMaskSevenCount;
        set => SetProperty(ref _minMaskSevenCount, value);
    }

    public bool TeamAutoEnabled
    {
        get => _teamAutoEnabled;
        set
        {
            if (SetProperty(ref _teamAutoEnabled, value))
            {
                if (value)
                {
                    foreach (var row in RowPriorities)
                    {
                        row.Choice = RowStatChoice.None;
                    }
                }

                RebuildAdventuringTeam();
            }
        }
    }

    public ObservableCollection<RowPrioritySetting> RowPriorities { get; }

    public ObservableCollection<AdventuringSlot> AdventuringTeam { get; }

    public IReadOnlyList<RowStatChoice> RowChoiceOptions { get; } =
        Enum.GetValues<RowStatChoice>().ToList();

    public DashboardSection ActiveSection
    {
        get => _activeSection;
        set => SetProperty(ref _activeSection, value);
    }

    public RelayCommand SetSectionCommand { get; }

    public async Task RefreshAsync()
    {
        SyncFromSettings();
        Cats.Clear();
        foreach (var cat in await _roster.GetCatsAsync())
        {
            Cats.Add(cat);
        }

        RaisePropertyChanged(nameof(TotalCats));
        RaisePropertyChanged(nameof(RetiredCount));
        RaisePropertyChanged(nameof(NaturalSevenCount));
        RebuildPlan();
    }

    public void ApplyConfig(DashboardConfig options)
    {
        TopCatCount = options.TopCatCount;
        BackfillsPerMask = options.BackfillsPerMask;
        MinMaskSevenCount = options.MinMaskSevenCount;
        RebuildPlan();
    }

    private void SyncFromSettings()
    {
        var cfg = AppServices.SettingsConfig;
        TopCatCount = cfg.TopCatCount;
        BackfillsPerMask = cfg.BackfillsPerMask;
        MinMaskSevenCount = cfg.MinMaskSevenCount;
    }

    public void RebuildPlan()
    {
        TopCatCount = Math.Max(1, TopCatCount);
        BackfillsPerMask = Math.Max(0, BackfillsPerMask);
        MinMaskSevenCount = Math.Max(0, MinMaskSevenCount);

        Plan = _advisor.BuildPlan(Cats, new BreedingPlanOptions(TopCatCount, BackfillsPerMask, MinMaskSevenCount));
        RebuildAdventuringTeam();
    }

    public void SortBreedingPool(string sortPath, ListSortDirection direction)
    {
        Func<BreedingPoolEntry, object?> keySelector = sortPath switch
        {
            "NameWithId" => entry => entry.Cat.Name,
            "StrSortKey" => entry => entry.StrSortKey,
            "DexSortKey" => entry => entry.DexSortKey,
            "StaSortKey" => entry => entry.StaSortKey,
            "IntSortKey" => entry => entry.IntSortKey,
            "SpdSortKey" => entry => entry.SpdSortKey,
            "ChaSortKey" => entry => entry.ChaSortKey,
            "LukSortKey" => entry => entry.LukSortKey,
            "Cat.BaseSevenCount" => entry => entry.Cat.BaseSevenCount,
            "CompatiblePartners" => entry => entry.CompatiblePartners,
            "Cat.BaseAverage" => entry => entry.Cat.BaseAverage,
            "Cat.CurrentAverage" => entry => entry.Cat.CurrentAverage,
            "Reason" => entry => entry.Reason,
            _ => entry => entry.Cat.Name
        };

        var sorted = direction == ListSortDirection.Descending
            ? Plan.BreedingPool.OrderByDescending(keySelector).ToList()
            : Plan.BreedingPool.OrderBy(keySelector).ToList();

        Plan = Plan with { BreedingPool = sorted };
    }

    private void RebuildAdventuringTeam()
    {
        AdventuringTeam.Clear();

        var candidates = Plan.GeneralPopulation
            .Select(entry => entry.Cat)
            .Where(cat => !cat.IsRetired)
            .ToList();

        var requests = RowPriorities
            .Select(row => new RowRequest(row.RowIndex, TeamAutoEnabled ? RowStatChoice.None : row.Choice))
            .ToList();

        var usedIds = new HashSet<int>();
        foreach (var req in requests)
        {
            var candidate = candidates
                .Where(cat => !usedIds.Contains(cat.Id))
                .Select(cat => new { Cat = cat, Score = ScoreForChoice(cat, req.Choice) })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Cat.CurrentAverage)
                .ThenByDescending(x => x.Cat.Id)
                .FirstOrDefault();

            if (candidate is null)
            {
                AdventuringTeam.Add(new AdventuringSlot(req.RowIndex, req.Choice, null, 0));
                continue;
            }

            usedIds.Add(candidate.Cat.Id);
            AdventuringTeam.Add(new AdventuringSlot(req.RowIndex, req.Choice, candidate.Cat, candidate.Score));
        }
    }

    private void OnRowPriorityChanged(RowPrioritySetting row)
    {
        if (TeamAutoEnabled)
        {
            return;
        }

        RebuildAdventuringTeam();
    }

    private static double ScoreForChoice(CatProfile cat, RowStatChoice choice)
    {
        return choice switch
        {
            RowStatChoice.Strength => cat.StrengthCurrent,
            RowStatChoice.Dexterity => cat.DexterityCurrent,
            RowStatChoice.Stamina => cat.StaminaCurrent,
            RowStatChoice.Intellect => cat.IntellectCurrent,
            RowStatChoice.Speed => cat.SpeedCurrent,
            RowStatChoice.Charisma => cat.CharismaCurrent,
            RowStatChoice.Luck => cat.LuckCurrent,
            _ => cat.CurrentAverage
        };
    }

    public void GoToManageCats()
    {
        _nav.NavigateTo(AppPage.ManageCats);
    }

    private void SetSection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (Enum.TryParse<DashboardSection>(value, out var section))
        {
            ActiveSection = section;
        }
    }

    public sealed class RowPrioritySetting : ViewModelBase
    {
        private RowStatChoice _choice;
        private readonly Action<RowPrioritySetting> _onChanged;

        public RowPrioritySetting(int rowIndex, RowStatChoice choice, Action<RowPrioritySetting> onChanged)
        {
            RowIndex = rowIndex;
            _choice = choice;
            _onChanged = onChanged;
        }

        public int RowIndex { get; }

        public RowStatChoice Choice
        {
            get => _choice;
            set
            {
                if (SetProperty(ref _choice, value))
                {
                    _onChanged(this);
                }
            }
        }
    }

    public sealed record AdventuringSlot(int Row, RowStatChoice Choice, CatProfile? Cat, double Score);
    private sealed record RowRequest(int RowIndex, RowStatChoice Choice);

    public enum RowStatChoice
    {
        None,
        Strength,
        Dexterity,
        Stamina,
        Intellect,
        Speed,
        Charisma,
        Luck
    }

    public enum DashboardSection
    {
        Snapshot,
        Adventuring,
        Breeding,
        General,
        FullRoster
    }

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

    private static IReadOnlyList<string> GetMissingStatNames(int mask)
    {
        var stats = new[]
        {
            (Bit: 0, Name: "STR"),
            (Bit: 1, Name: "DEX"),
            (Bit: 2, Name: "STA"),
            (Bit: 3, Name: "INT"),
            (Bit: 4, Name: "SPD"),
            (Bit: 5, Name: "CHA"),
            (Bit: 6, Name: "LCK")
        };

        return stats
            .Where(x => (mask & (1 << x.Bit)) == 0)
            .Select(x => x.Name)
            .ToList();
    }
}
