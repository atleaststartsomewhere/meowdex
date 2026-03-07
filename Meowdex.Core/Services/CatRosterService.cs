using System.Text.Json;
using Meowdex.Core.Models;

namespace Meowdex.Core.Services;

public sealed class CatRosterService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly Dictionary<int, List<CatProfile>> _debugSessionCats = [];
    private int _activeProfile = 1;

    public int ActiveProfile => _activeProfile;

    public void SetActiveProfile(int profileId)
    {
        _activeProfile = NormalizeProfile(profileId);
    }

    public async Task<IReadOnlyList<CatProfile>> GetCatsAsync()
    {
        await _mutex.WaitAsync();

        try
        {
            var cats = await LoadUnsafeAsync();
            return cats.OrderByDescending(cat => cat.Id).ToList();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<CatProfile?> GetByIdAsync(int id)
    {
        await _mutex.WaitAsync();

        try
        {
            return (await LoadUnsafeAsync()).FirstOrDefault(cat => cat.Id == id)?.Clone();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<CatProfile> AddCatAsync(CatProfile input)
    {
        await _mutex.WaitAsync();

        try
        {
            var cats = (await LoadUnsafeAsync()).ToList();
            var nextId = cats.Count == 0 ? 1 : cats.Max(cat => cat.Id) + 1;

            var created = Sanitize(input).WithId(nextId);
            cats.Add(created);
            await SaveUnsafeAsync(cats);
            return created.Clone();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyList<CatProfile>> AddCatsAsync(IEnumerable<CatProfile> inputs)
    {
        await _mutex.WaitAsync();

        try
        {
            var cats = (await LoadUnsafeAsync()).ToList();
            var nextId = cats.Count == 0 ? 1 : cats.Max(cat => cat.Id) + 1;
            var created = new List<CatProfile>();

            foreach (var input in inputs)
            {
                var cat = Sanitize(input).WithId(nextId++);
                cats.Add(cat);
                created.Add(cat.Clone());
            }

            if (created.Count > 0)
            {
                await SaveUnsafeAsync(cats);
            }

            return created;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<bool> UpdateCatAsync(CatProfile input)
    {
        await _mutex.WaitAsync();

        try
        {
            var cats = (await LoadUnsafeAsync()).ToList();
            var index = cats.FindIndex(cat => cat.Id == input.Id);
            if (index < 0)
            {
                return false;
            }

            cats[index] = Sanitize(input).WithId(input.Id);
            await SaveUnsafeAsync(cats);
            return true;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task DeleteCatAsync(int catId)
    {
        await _mutex.WaitAsync();

        try
        {
            var cats = (await LoadUnsafeAsync())
                .Where(cat => cat.Id != catId)
                .ToList();

            await SaveUnsafeAsync(cats);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<IReadOnlyList<CatProfile>> LoadUnsafeAsync()
    {
        var profileId = NormalizeProfile(_activeProfile);
        if (IsDebugSession())
        {
            if (_debugSessionCats.TryGetValue(profileId, out var cached))
            {
                return CloneList(cached);
            }

            var seeded = await LoadPersistentUnsafeAsync(profileId, migrateLegacy: false, fallbackLegacyForProfileOne: true);
            _debugSessionCats[profileId] = CloneList(seeded);
            return CloneList(seeded);
        }

        return await LoadPersistentUnsafeAsync(profileId, migrateLegacy: true, fallbackLegacyForProfileOne: false);
    }

    private async Task SaveUnsafeAsync(IReadOnlyList<CatProfile> cats)
    {
        var profileId = NormalizeProfile(_activeProfile);
        if (IsDebugSession())
        {
            // In debug mode, never persist to disk; keep all changes in-memory for this process only.
            _debugSessionCats[profileId] = CloneList(cats);
            return;
        }

        var storePath = ResolveStorePath(profileId);
        var folder = Path.GetDirectoryName(storePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        await using var stream = File.Create(storePath);
        await JsonSerializer.SerializeAsync(stream, cats, JsonOptions);
    }

    private static async Task<IReadOnlyList<CatProfile>> LoadPersistentUnsafeAsync(int profileId, bool migrateLegacy, bool fallbackLegacyForProfileOne)
    {
        var storePath = ResolveStorePath(profileId);
        if (migrateLegacy)
        {
            EnsureLegacyProfileOneMigration(profileId, storePath);
        }

        if (File.Exists(storePath))
        {
            await using var stream = File.OpenRead(storePath);
            var loaded = await JsonSerializer.DeserializeAsync<List<CatProfile>>(stream, JsonOptions);
            return loaded ?? [];
        }

        if (fallbackLegacyForProfileOne && NormalizeProfile(profileId) == 1)
        {
            var legacyPath = ResolveLegacyStorePath();
            if (File.Exists(legacyPath))
            {
                await using var legacyStream = File.OpenRead(legacyPath);
                var legacyLoaded = await JsonSerializer.DeserializeAsync<List<CatProfile>>(legacyStream, JsonOptions);
                return legacyLoaded ?? [];
            }
        }

        return [];
    }

    private static string ResolveStorePath(int profileId)
    {
        var normalizedProfile = NormalizeProfile(profileId);
        var appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appDataRoot, "Meowdex", $"Profile{normalizedProfile}");
        return Path.Combine(folder, "cats.json");
    }

    private static int NormalizeProfile(int profileId) => profileId is >= 1 and <= 3 ? profileId : 1;

    private static void EnsureLegacyProfileOneMigration(int profileId, string targetStorePath)
    {
        if (NormalizeProfile(profileId) != 1 || File.Exists(targetStorePath))
        {
            return;
        }

        var legacyPath = ResolveLegacyStorePath();
        if (!File.Exists(legacyPath))
        {
            return;
        }

        var folder = Path.GetDirectoryName(targetStorePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.Copy(legacyPath, targetStorePath, overwrite: false);
    }

    private static string ResolveLegacyStorePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(root, "Meowdex", "cats.json");
    }

    private static bool IsDebugSession()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }

    private static List<CatProfile> CloneList(IEnumerable<CatProfile> cats)
    {
        return cats.Select(cat => cat.Clone()).ToList();
    }

    private static CatProfile Sanitize(CatProfile cat)
    {
        var clean = cat.Clone();
        clean.Name = clean.Name.Trim();
        clean.Notes = clean.Notes.Trim();

        clean.StrengthBase = CatProfile.ClampBase(clean.StrengthBase);
        clean.DexterityBase = CatProfile.ClampBase(clean.DexterityBase);
        clean.StaminaBase = CatProfile.ClampBase(clean.StaminaBase);
        clean.IntellectBase = CatProfile.ClampBase(clean.IntellectBase);
        clean.SpeedBase = CatProfile.ClampBase(clean.SpeedBase);
        clean.CharismaBase = CatProfile.ClampBase(clean.CharismaBase);
        clean.LuckBase = CatProfile.ClampBase(clean.LuckBase);

        clean.StrengthCurrent = CatProfile.ClampCurrent(clean.StrengthCurrent);
        clean.DexterityCurrent = CatProfile.ClampCurrent(clean.DexterityCurrent);
        clean.StaminaCurrent = CatProfile.ClampCurrent(clean.StaminaCurrent);
        clean.IntellectCurrent = CatProfile.ClampCurrent(clean.IntellectCurrent);
        clean.SpeedCurrent = CatProfile.ClampCurrent(clean.SpeedCurrent);
        clean.CharismaCurrent = CatProfile.ClampCurrent(clean.CharismaCurrent);
        clean.LuckCurrent = CatProfile.ClampCurrent(clean.LuckCurrent);

        return clean;
    }
}
