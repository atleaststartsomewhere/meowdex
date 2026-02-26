using System.Text.Json;
using Meowdex.Desktop.ViewModels;

namespace Meowdex.Desktop.Services;

public sealed class PlayerProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _storePath = ResolveStorePath();
    private readonly object _sync = new();

    public PlayerProfileState Load()
    {
        lock (_sync)
        {
            if (!File.Exists(_storePath))
            {
                var defaults = PlayerProfileState.CreateDefault();
                SaveUnsafe(defaults);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(_storePath);
                var loaded = JsonSerializer.Deserialize<PlayerProfileState>(json, JsonOptions);
                var normalized = Normalize(loaded ?? PlayerProfileState.CreateDefault());
                SaveUnsafe(normalized);
                return normalized;
            }
            catch
            {
                var defaults = PlayerProfileState.CreateDefault();
                SaveUnsafe(defaults);
                return defaults;
            }
        }
    }

    public void Save(PlayerProfileState state)
    {
        lock (_sync)
        {
            SaveUnsafe(Normalize(state));
        }
    }

    private void SaveUnsafe(PlayerProfileState state)
    {
        var folder = Path.GetDirectoryName(_storePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_storePath, json);
    }

    private static PlayerProfileState Normalize(PlayerProfileState state)
    {
        var normalized = new PlayerProfileState
        {
            ActiveProfile = NormalizeProfile(state.ActiveProfile),
            Profile1 = state.Profile1 ?? DefaultConfig(),
            Profile2 = state.Profile2 ?? DefaultConfig(),
            Profile3 = state.Profile3 ?? DefaultConfig()
        };

        return normalized;
    }

    private static int NormalizeProfile(int profileId) => profileId is >= 1 and <= 3 ? profileId : 1;

    private static DashboardConfig DefaultConfig() => new(3, 1, 0);

    private static string ResolveStorePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(root, "Meowdex", "profiles.json");
    }
}

public sealed class PlayerProfileState
{
    public int ActiveProfile { get; set; } = 1;
    public DashboardConfig? Profile1 { get; set; } = new(3, 1, 0);
    public DashboardConfig? Profile2 { get; set; } = new(3, 1, 0);
    public DashboardConfig? Profile3 { get; set; } = new(3, 1, 0);

    public DashboardConfig GetConfig(int profileId)
    {
        return profileId switch
        {
            2 => Profile2 ?? new DashboardConfig(3, 1, 0),
            3 => Profile3 ?? new DashboardConfig(3, 1, 0),
            _ => Profile1 ?? new DashboardConfig(3, 1, 0)
        };
    }

    public void SetConfig(int profileId, DashboardConfig config)
    {
        switch (profileId)
        {
            case 2:
                Profile2 = config;
                break;
            case 3:
                Profile3 = config;
                break;
            default:
                Profile1 = config;
                break;
        }
    }

    public static PlayerProfileState CreateDefault() => new();
}
