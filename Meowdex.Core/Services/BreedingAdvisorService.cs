using Meowdex.Core.Models;
using System.Globalization;

namespace Meowdex.Core.Services;

public sealed class BreedingAdvisorService
{
    public BreedingPlanResult BuildPlan(IReadOnlyList<CatProfile> cats, BreedingPlanOptions? options = null)
    {
        var cfg = options ?? new BreedingPlanOptions();
        var topCount = Math.Max(1, cfg.TopCatCount);
        var partnersPerTopCat = Math.Max(0, cfg.BackfillsPerMask);
        var minSevenCount = Math.Max(0, cfg.MinSevenCount);

        var activeCats = cats.ToList();

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
        var partnerRecommendations = BuildTopBreederPartnerRecommendations(
            topCats,
            activeCats,
            partnersPerTopCat,
            coveredMask);

        var partnerCats = partnerRecommendations
            .SelectMany(x => x.Partners)
            .Select(x => x.Partner)
            .DistinctBy(cat => cat.Id)
            .Where(cat => topCats.All(top => top.Id != cat.Id))
            .ToList();

        foreach (var partner in partnerCats)
        {
            breedingPool.Add(partner);
            coveredMask |= partner.SevenMask;
        }

        var breedingIds = breedingPool.Select(cat => cat.Id).ToHashSet();
        var poolCompatibleCounts = breedingPool.ToDictionary(
            cat => cat.Id,
            cat => breedingPool.Count(other => other.Id != cat.Id && CanBreed(cat, other)));

        var additionalDiversityCats = partnerCats;
        var partnerReasons = BuildPartnerReasonMap(partnerRecommendations);

        var breedingEntries = breedingPool
            .Select(cat => new BreedingPoolEntry(
                cat,
                FormatMask(cat.SevenMask),
                poolCompatibleCounts[cat.Id],
                topCats.Contains(cat)
                    ? "Top-mask priority"
                    : partnerReasons.GetValueOrDefault(cat.Id, "Top-breeder partner recommendation")))
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
            partnersPerTopCat,
            breedingEntries,
            topCats,
            additionalDiversityCats,
            generalPopulation,
            coveredMask,
            partnerRecommendations);
    }

    public static bool CanBreed(CatProfile a, CatProfile b)
    {
        if (a.Id == b.Id)
        {
            return false;
        }

        return IsAttractedTo(a, b.Gender) && IsAttractedTo(b, a.Gender);
    }

    public static string FormatMask(int mask)
    {
        return Convert.ToString(mask, 2).PadLeft(7, '0');
    }

    private static bool AddsNovelCoverage(int coveredMask, int candidateMask)
    {
        return (coveredMask | candidateMask) != coveredMask;
    }

    private static IReadOnlyList<TopBreederRecommendation> BuildTopBreederPartnerRecommendations(
        IReadOnlyList<CatProfile> topCats,
        IReadOnlyList<CatProfile> allCats,
        int partnersPerTopCat,
        int initialCoveredMask)
    {
        if (partnersPerTopCat == 0 || topCats.Count == 0)
        {
            return [];
        }

        var coveredMask = initialCoveredMask;
        var partnerUseCounts = new Dictionary<int, int>();
        var recommendations = new List<TopBreederRecommendation>();

        foreach (var breeder in topCats)
        {
            var selected = new List<PartnerRecommendation>();

            for (var i = 0; i < partnersPerTopCat; i++)
            {
                var best = allCats
                    .Where(candidate => candidate.Id != breeder.Id && selected.All(x => x.Partner.Id != candidate.Id))
                    .Select(candidate => new
                    {
                        Partner = candidate,
                        Score = ScorePair(breeder, candidate, coveredMask, partnerUseCounts.GetValueOrDefault(candidate.Id))
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Partner.BaseSevenCount)
                    .ThenByDescending(x => x.Partner.BaseAverage)
                    .ThenByDescending(x => x.Partner.Id)
                    .FirstOrDefault();

                if (best is null)
                {
                    break;
                }

                var novelMask = best.Partner.SevenMask & ~coveredMask;
                var relationMask = best.Partner.SevenMask & ~breeder.SevenMask;
                selected.Add(new PartnerRecommendation(best.Partner, best.Score, novelMask, relationMask));

                coveredMask |= best.Partner.SevenMask;
                partnerUseCounts[best.Partner.Id] = partnerUseCounts.GetValueOrDefault(best.Partner.Id) + 1;
            }

            recommendations.Add(new TopBreederRecommendation(breeder, selected));
        }

        return recommendations;
    }

    private static Dictionary<int, string> BuildPartnerReasonMap(IReadOnlyList<TopBreederRecommendation> recommendations)
    {
        var byPartner = recommendations
            .SelectMany(rec => rec.Partners.Select(partner => (Breeder: rec.Breeder, Partner: partner)))
            .GroupBy(x => x.Partner.Partner.Id)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var breeders = g.Select(x => $"#{x.Breeder.Id}").Distinct().ToList();
                    return $"Partner for {string.Join(", ", breeders)}";
                });

        return byPartner;
    }

    private static double ScorePair(CatProfile breeder, CatProfile partner, int coveredMask, int reuseCount)
    {
        if (!CanBreed(breeder, partner))
        {
            return 0;
        }

        var novelPoolBits = CountBits(partner.SevenMask & ~coveredMask);
        var novelBreederBits = CountBits(partner.SevenMask & ~breeder.SevenMask);
        var quality = partner.BaseAverage / 7.0;
        var reusePenalty = reuseCount * 0.75;

        return
            (novelPoolBits * 4.0) +
            (novelBreederBits * 2.0) +
            (partner.BaseSevenCount * 0.5) +
            (quality * 0.5) -
            reusePenalty;
    }

    private static bool IsAttractedTo(CatProfile cat, CatGender targetGender)
    {
        return cat.Sexuality switch
        {
            CatSexuality.Bi => true,
            CatSexuality.Straight => IsStraightAttractedTo(cat.Gender, targetGender),
            CatSexuality.GayLesbian => IsGayAttractedTo(cat.Gender, targetGender),
            _ => false
        };
    }

    private static bool IsStraightAttractedTo(CatGender ownGender, CatGender targetGender)
    {
        return ownGender switch
        {
            CatGender.Male => targetGender is CatGender.Female or CatGender.Fluid,
            CatGender.Female => targetGender is CatGender.Male or CatGender.Fluid,
            CatGender.Fluid => targetGender is not CatGender.Fluid,
            _ => false
        };
    }

    private static bool IsGayAttractedTo(CatGender ownGender, CatGender targetGender)
    {
        return ownGender switch
        {
            CatGender.Male => targetGender is CatGender.Male or CatGender.Fluid,
            CatGender.Female => targetGender is CatGender.Female or CatGender.Fluid,
            CatGender.Fluid => targetGender is CatGender.Fluid,
            _ => false
        };
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
    public string GenderSortKey => Cat.Gender.ToString();
    public string SexualitySortKey => Cat.Sexuality switch
    {
        CatSexuality.Bi => "Bisexual",
        CatSexuality.GayLesbian => "Gay / Lesbian",
        CatSexuality.Straight => "Straight",
        _ => string.Empty
    };

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
    int CoveredMask,
    IReadOnlyList<TopBreederRecommendation>? TopBreederRecommendations = null);

public sealed record TopBreederRecommendation(
    CatProfile Breeder,
    IReadOnlyList<PartnerRecommendation> Partners);

public sealed record PartnerRecommendation(
    CatProfile Partner,
    double Score,
    int NovelPoolMask,
    int NovelForBreederMask);
