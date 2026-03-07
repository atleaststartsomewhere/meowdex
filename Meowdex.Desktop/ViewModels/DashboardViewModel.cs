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
    private readonly HashSet<int> _forcedAvailableCatIds = [];
    private readonly HashSet<int> _forcedBreedingCatIds = [];
    private readonly List<int> _pinnedTeamCatIds = [];
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
    public string NaturalSevenSummary =>
        TotalCats == 0
            ? "Natural 7 cats: 0/0 (0%)"
            : $"Natural 7 cats: {NaturalSevenCount}/{TotalCats} ({(NaturalSevenCount * 100.0 / TotalCats):F0}%)";
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
    public int AdventuringTeamCount => AdventuringTeam.Count(slot => slot.Cat is not null);

    public IReadOnlyList<RowStatChoice> RowChoiceOptions { get; } =
        Enum.GetValues<RowStatChoice>().ToList();
    public IReadOnlyList<string> RowChoiceLabels { get; } =
        Enum.GetValues<RowStatChoice>().Select(ToChoiceLabel).ToList();

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
        RaisePropertyChanged(nameof(NaturalSevenSummary));
        RebuildPlan();
    }

    public void ClearCurrentTeam()
    {
        TeamAutoEnabled = false;
        _pinnedTeamCatIds.Clear();
        foreach (var row in RowPriorities)
        {
            row.Choice = RowStatChoice.None;
        }

        RebuildAdventuringTeam();
    }

    public async Task RetireCurrentTeamAsync()
    {
        var teamCats = AdventuringTeam
            .Where(slot => slot.Cat is not null && !slot.Cat.IsRetired)
            .Select(slot => slot.Cat!)
            .DistinctBy(cat => cat.Id)
            .ToList();

        foreach (var cat in teamCats)
        {
            var updated = cat.Clone();
            updated.IsRetired = true;
            await _roster.UpdateCatAsync(updated);
        }

        await RefreshAsync();
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
        ApplyManualPoolOverrides();
        RebuildAdventuringTeam();
    }

    public void SortBreedingPool(string sortPath, ListSortDirection direction)
    {
        Func<BreedingPoolEntry, object?> keySelector = sortPath switch
        {
            "Id" => entry => entry.Cat.Id,
            "NameWithId" => entry => entry.Cat.Name,
            "GenderSortKey" => entry => entry.GenderSortKey,
            "SexualitySortKey" => entry => entry.SexualitySortKey,
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

    public void RemoveFromBreedingPool(int catId)
    {
        _forcedBreedingCatIds.Remove(catId);
        _forcedAvailableCatIds.Add(catId);
        ApplyManualPoolOverrides();
        RebuildAdventuringTeam();
    }

    public void SendToBreedingPool(int catId)
    {
        _forcedAvailableCatIds.Remove(catId);
        _forcedBreedingCatIds.Add(catId);
        ApplyManualPoolOverrides();
        RebuildAdventuringTeam();
    }

    public void SendToAdventuringTeam(int catId)
    {
        var candidate = Cats.FirstOrDefault(cat => cat.Id == catId);
        if (candidate is null || candidate.IsRetired)
        {
            return;
        }

        if (_pinnedTeamCatIds.Contains(catId))
        {
            return;
        }

        if (_pinnedTeamCatIds.Count >= 4)
        {
            return;
        }

        _pinnedTeamCatIds.Add(catId);
        RebuildAdventuringTeam();
    }

    private void RebuildAdventuringTeam()
    {
        AdventuringTeam.Clear();

        var candidates = Plan.GeneralPopulation
            .Select(entry => entry.Cat)
            .Where(cat => !cat.IsRetired)
            .ToList();

        _pinnedTeamCatIds.RemoveAll(id => Cats.All(cat => cat.Id != id || cat.IsRetired));
        var pinnedCats = _pinnedTeamCatIds
            .Select(id => Cats.FirstOrDefault(cat => cat.Id == id && !cat.IsRetired))
            .Where(cat => cat is not null)
            .Cast<CatProfile>()
            .ToList();

        var requests = RowPriorities
            .Select(row => new RowRequest(row.RowIndex, TeamAutoEnabled ? RowStatChoice.None : row.Choice))
            .ToList();

        var usedIds = new HashSet<int>();
        for (var i = 0; i < requests.Count; i++)
        {
            var req = requests[i];

            if (i < pinnedCats.Count)
            {
                var pinned = pinnedCats[i];
                usedIds.Add(pinned.Id);
                AdventuringTeam.Add(new AdventuringSlot(req.RowIndex, req.Choice, pinned, ScoreForChoice(pinned, req.Choice)));
                continue;
            }

            if (!TeamAutoEnabled && req.Choice == RowStatChoice.None)
            {
                AdventuringTeam.Add(new AdventuringSlot(req.RowIndex, req.Choice, null, 0));
                continue;
            }

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

        RaisePropertyChanged(nameof(AdventuringTeamCount));

        var slotsByRow = AdventuringTeam.ToDictionary(slot => slot.Row);
        foreach (var row in RowPriorities)
        {
            row.AssignedCatName = slotsByRow.TryGetValue(row.RowIndex, out var slot) && slot.Cat is not null
                ? slot.Cat.Name
                : "No cat selected";
        }
    }

    private void ApplyManualPoolOverrides()
    {
        if (_forcedAvailableCatIds.Count == 0 && _forcedBreedingCatIds.Count == 0)
        {
            return;
        }

        _forcedAvailableCatIds.RemoveWhere(id => Cats.All(cat => cat.Id != id));
        _forcedBreedingCatIds.RemoveWhere(id => Cats.All(cat => cat.Id != id));

        if (_forcedAvailableCatIds.Count == 0 && _forcedBreedingCatIds.Count == 0)
        {
            return;
        }

        var reasonsByPoolId = Plan.BreedingPool.ToDictionary(entry => entry.Cat.Id, entry => entry.Reason);

        var basePoolEntries = Plan.BreedingPool
            .Where(entry => !_forcedAvailableCatIds.Contains(entry.Cat.Id))
            .ToList();

        var poolCatsById = basePoolEntries
            .Select(entry => entry.Cat)
            .ToDictionary(cat => cat.Id, cat => cat);

        foreach (var forcedBreedingId in _forcedBreedingCatIds)
        {
            if (poolCatsById.ContainsKey(forcedBreedingId))
            {
                continue;
            }

            var cat = Cats.FirstOrDefault(x => x.Id == forcedBreedingId);
            if (cat is null)
            {
                continue;
            }

            poolCatsById[cat.Id] = cat;
            reasonsByPoolId[cat.Id] = "Manually added to breeding pool";
        }

        var finalPoolCats = poolCatsById.Values
            .ToList();

        var poolIds = finalPoolCats.Select(cat => cat.Id).ToHashSet();

        var generalEntriesById = Plan.GeneralPopulation
            .Where(entry => !poolIds.Contains(entry.Cat.Id))
            .ToDictionary(entry => entry.Cat.Id, entry => entry);

        foreach (var cat in Cats.Where(cat => !poolIds.Contains(cat.Id)))
        {
            if (!generalEntriesById.ContainsKey(cat.Id))
            {
                generalEntriesById[cat.Id] = new GeneralPopulationEntry(
                    cat,
                    GetCompatiblePartners(cat),
                    "Manually removed from breeding pool");
            }
        }

        var poolCompatibleCounts = finalPoolCats.ToDictionary(
            cat => cat.Id,
            cat => finalPoolCats.Count(other => other.Id != cat.Id && BreedingAdvisorService.CanBreed(cat, other)));

        var finalPoolEntries = finalPoolCats
            .Select(cat => new BreedingPoolEntry(
                cat,
                BreedingAdvisorService.FormatMask(cat.SevenMask),
                poolCompatibleCounts[cat.Id],
                reasonsByPoolId.GetValueOrDefault(cat.Id, "Manually added to breeding pool")))
            .ToList();

        var coveredMask = finalPoolCats.Aggregate(0, (mask, cat) => mask | cat.SevenMask);

        Plan = Plan with
        {
            BreedingPool = finalPoolEntries,
            GeneralPopulation = generalEntriesById.Values
                .OrderByDescending(entry => entry.Cat.CurrentAverage)
                .ToList(),
            CoveredMask = coveredMask,
            TopBreederRecommendations = Plan.TopBreederRecommendations?
                .Select(rec => rec with
                {
                    Partners = rec.Partners
                        .Where(partner => !_forcedAvailableCatIds.Contains(partner.Partner.Id))
                        .ToList()
                })
                .Where(rec => !_forcedAvailableCatIds.Contains(rec.Breeder.Id) && !_forcedBreedingCatIds.Contains(rec.Breeder.Id))
                .ToList()
        };
    }

    private int GetCompatiblePartners(CatProfile cat)
    {
        return Cats.Count(other => other.Id != cat.Id && BreedingAdvisorService.CanBreed(cat, other));
    }

    private void OnRowPriorityChanged(RowPrioritySetting row)
    {
        if (TeamAutoEnabled && row.Choice != RowStatChoice.None)
        {
            TeamAutoEnabled = false;
            return;
        }

        RebuildAdventuringTeam();
    }

    private static string ToChoiceLabel(RowStatChoice choice)
    {
        return choice == RowStatChoice.None ? "Auto" : choice.ToString();
    }

    private static RowStatChoice FromChoiceLabel(string? label)
    {
        if (string.Equals(label, "Auto", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(label))
        {
            return RowStatChoice.None;
        }

        return Enum.TryParse<RowStatChoice>(label, true, out var parsed) ? parsed : RowStatChoice.None;
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
        private string _assignedCatName = "No cat selected";

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
                    RaisePropertyChanged(nameof(ChoiceLabel));
                    _onChanged(this);
                }
            }
        }

        public string ChoiceLabel
        {
            get => ToChoiceLabel(Choice);
            set => Choice = FromChoiceLabel(value);
        }

        public string AssignedCatName
        {
            get => _assignedCatName;
            set => SetProperty(ref _assignedCatName, value);
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
        Available,
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
