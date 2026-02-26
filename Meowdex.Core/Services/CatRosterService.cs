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
        var storePath = ResolveStorePath(_activeProfile);
        EnsureLegacyProfileOneMigration(_activeProfile, storePath);
        if (!File.Exists(storePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(storePath);
        var cats = await JsonSerializer.DeserializeAsync<List<CatProfile>>(stream, JsonOptions);
        return cats ?? [];
    }

    private async Task SaveUnsafeAsync(IReadOnlyList<CatProfile> cats)
    {
        var storePath = ResolveStorePath(_activeProfile);
        var folder = Path.GetDirectoryName(storePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        await using var stream = File.Create(storePath);
        await JsonSerializer.SerializeAsync(stream, cats, JsonOptions);
    }

    private static string ResolveStorePath(int profileId)
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(root, "Meowdex", $"Profile{NormalizeProfile(profileId)}");
        return Path.Combine(folder, "cats.json");
    }

    private static int NormalizeProfile(int profileId) => profileId is >= 1 and <= 3 ? profileId : 1;

    private static void EnsureLegacyProfileOneMigration(int profileId, string targetStorePath)
    {
        if (NormalizeProfile(profileId) != 1 || File.Exists(targetStorePath))
        {
            return;
        }

        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var legacyPath = Path.Combine(root, "Meowdex", "cats.json");
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
