using System.Text.Json;
using System.Text.RegularExpressions;

namespace PrTray.Core.Storage;

public sealed partial class ConfigStore(string filePath)
{
    public const int MinimumPollIntervalSeconds = 30;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public PrTrayConfig LoadOrCreate()
    {
        if (!File.Exists(filePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, JsonSerializer.Serialize(PrTrayConfig.Default, JsonOptions));
            return PrTrayConfig.Default;
        }
        try
        {
            var stored = JsonSerializer.Deserialize<StoredConfig>(File.ReadAllText(filePath), JsonOptions);
            return stored is null ? PrTrayConfig.Default : Sanitize(stored);
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($"PrTray: ignoring invalid {filePath}: {exception.Message}");
            return PrTrayConfig.Default;
        }
    }

    private static PrTrayConfig Sanitize(StoredConfig stored) => new(
        WatchedRepositories: (stored.WatchedRepositories ?? [])
            .OfType<string>()
            .Where(repository => RepositoryName().IsMatch(repository))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList(),
        PollIntervalSeconds: Math.Max(stored.PollIntervalSeconds ?? PrTrayConfig.Default.PollIntervalSeconds, MinimumPollIntervalSeconds),
        GhPath: string.IsNullOrWhiteSpace(stored.GhPath) ? PrTrayConfig.Default.GhPath : stored.GhPath);

    [GeneratedRegex("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")]
    private static partial Regex RepositoryName();

    internal sealed record StoredConfig(List<string?>? WatchedRepositories, int? PollIntervalSeconds, string? GhPath);
}
