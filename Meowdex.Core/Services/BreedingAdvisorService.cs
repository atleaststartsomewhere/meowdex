using Meowdex.Core.Models;
using System.Globalization;

namespace Meowdex.Core.Services;

public sealed class BreedingAdvisorService
{
    public BreedingPlanResult BuildPlan(IReadOnlyList<CatProfile> cats, BreedingPlanOptions? options = null)
    {
        var cfg = options ?? new BreedingPlanOptions();
        var topCount = Math.Max(1, cfg.TopCatCount);
        var backfillsPerMask = Math.Max(0, cfg.BackfillsPerMask);
        var minSevenCount = Math.Max(0, cfg.MinSevenCount);

        var activeCats = cats.Where(cat => !cat.IsRetired).ToList();

        var compatibleCounts = activeCats.ToDictionary(
            cat => cat.Id,
            cat => activeCats.Count(other => other.Id != cat.Id && CanBreed(cat, other)));

        var eligible = activeCats
            .Where(cat => cat.HasNaturalSeven && cat.BaseSevenCount >= minSevenCount)
            .ToList();

        var rankedForBreeding = eligible
            .OrderByDescending(cat => cat.BaseSevenCount)
            .ThenByDescending(cat => compatibleCounts[cat.Id])
            .ThenByDescending(cat => cat.BaseAverage)
            .ThenByDescending(cat => cat.Id)
            .ToList();

        var topCats = rankedForBreeding.Take(topCount).ToList();

        var breedingPool = topCats.ToList();
        var coveredMask = topCats.Aggregate(0, (mask, cat) => mask | cat.SevenMask);

        var topMasks = topCats.Select(cat => cat.SevenMask).ToHashSet();
        var backfillByMask = new Dictionary<int, int>();

        if (backfillsPerMask > 0)
        {
            foreach (var cat in rankedForBreeding.Skip(topCount))
            {
                var mask = cat.SevenMask;
                if (topMasks.Contains(mask))
                {
                    continue;
                }

                if (!AddsNovelCoverage(coveredMask, mask))
                {
                    continue;
                }

                backfillByMask.TryGetValue(mask, out var currentCount);
                if (currentCount >= backfillsPerMask)
                {
                    continue;
                }

                breedingPool.Add(cat);
                backfillByMask[mask] = currentCount + 1;
                coveredMask |= mask;
            }
        }

        var breedingIds = breedingPool.Select(cat => cat.Id).ToHashSet();

        var additionalDiversityCats = breedingPool
            .Where(cat => !topCats.Contains(cat))
            .ToList();

        var breedingEntries = breedingPool
            .Select(cat => new BreedingPoolEntry(
                cat,
                FormatMask(cat.SevenMask),
                compatibleCounts[cat.Id],
                topCats.Contains(cat) ? "Top-mask priority" : "Diversity backfill"))
            .ToList();

        var generalPopulation = cats
            .Where(cat => !breedingIds.Contains(cat.Id))
            .OrderByDescending(cat => cat.CurrentAverage)
            .Select(cat => new GeneralPopulationEntry(
                cat,
                cat.IsRetired
                    ? "Retired"
                    : cat.HasNaturalSeven ? "Outside selected breeding set" : "No natural base 7"))
            .ToList();

        return new BreedingPlanResult(
            topCount,
            backfillsPerMask,
            breedingEntries,
            topCats,
            additionalDiversityCats,
            generalPopulation,
            coveredMask);
    }

    public static bool CanBreed(CatProfile a, CatProfile b)
    {
        return a.Id != b.Id && a.Gender != b.Gender;
    }

    public static string FormatMask(int mask)
    {
        return Convert.ToString(mask, 2).PadLeft(7, '0');
    }

    private static bool AddsNovelCoverage(int coveredMask, int candidateMask)
    {
        return (coveredMask | candidateMask) != coveredMask;
    }
}

public sealed record BreedingPlanOptions(int TopCatCount = 3, int BackfillsPerMask = 1, int MinSevenCount = 0);

public sealed record BreedingPoolEntry(
    CatProfile Cat,
    string SevenMask,
    int CompatiblePartners,
    string Reason)
{
    public string NameWithId => Cat.Name;
    public int Id => Cat.Id;

    public bool Str => HasBit(Cat.SevenMask, 0);
    public bool Dex => HasBit(Cat.SevenMask, 1);
    public bool Sta => HasBit(Cat.SevenMask, 2);
    public bool Int => HasBit(Cat.SevenMask, 3);
    public bool Spd => HasBit(Cat.SevenMask, 4);
    public bool Cha => HasBit(Cat.SevenMask, 5);
    public bool Luk => HasBit(Cat.SevenMask, 6);

    public string StrMark => Str ? "✓" : string.Empty;
    public string DexMark => Dex ? "✓" : string.Empty;
    public string StaMark => Sta ? "✓" : string.Empty;
    public string IntMark => Int ? "✓" : string.Empty;
    public string SpdMark => Spd ? "✓" : string.Empty;
    public string ChaMark => Cha ? "✓" : string.Empty;
    public string LukMark => Luk ? "✓" : string.Empty;

    public string StrSortKey => BuildBitSortKey(Str, Cat);
    public string DexSortKey => BuildBitSortKey(Dex, Cat);
    public string StaSortKey => BuildBitSortKey(Sta, Cat);
    public string IntSortKey => BuildBitSortKey(Int, Cat);
    public string SpdSortKey => BuildBitSortKey(Spd, Cat);
    public string ChaSortKey => BuildBitSortKey(Cha, Cat);
    public string LukSortKey => BuildBitSortKey(Luk, Cat);

    private static bool HasBit(int mask, int bitIndex) => (mask & (1 << bitIndex)) != 0;

    private static string BuildBitSortKey(bool hasBit, CatProfile cat)
    {
        var bitRank = hasBit ? 0 : 1;
        var baseRank = 1000 - cat.BaseAverage;
        var currentRank = 1000 - cat.CurrentAverage;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{bitRank:D1}|{baseRank:0000.000}|{currentRank:0000.000}|{cat.Name}");
    }
}

public sealed record GeneralPopulationEntry(
    CatProfile Cat,
    string Reason);

public sealed record BreedingPlanResult(
    int TopCatCount,
    int BackfillsPerMask,
    IReadOnlyList<BreedingPoolEntry> BreedingPool,
    IReadOnlyList<CatProfile> TopCats,
    IReadOnlyList<CatProfile> AdditionalDiversityCats,
    IReadOnlyList<GeneralPopulationEntry> GeneralPopulation,
    int CoveredMask);
