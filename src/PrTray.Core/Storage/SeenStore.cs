using System.Text.Json;
using PrTray.Core.Detection;

namespace PrTray.Core.Storage;

public sealed class SeenStore(string filePath)
{
    public SeenState Load()
    {
        try
        {
            var stored = JsonSerializer.Deserialize<StoredSeen>(File.ReadAllText(filePath));
            return stored is { Initialized: true, Keys: not null }
                ? new SeenState(stored.Keys.ToHashSet(), IsFirstRun: false)
                : SeenState.FirstRun;
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException or JsonException)
        {
            return SeenState.FirstRun;
        }
    }

    public void Save(IReadOnlySet<string> keys)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var temporaryPath = filePath + ".tmp";
        var stored = new StoredSeen(Initialized: true, Keys: keys.Order(StringComparer.Ordinal).ToList());
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(stored));
        File.Move(temporaryPath, filePath, overwrite: true);
    }

    internal sealed record StoredSeen(bool Initialized, List<string>? Keys);
}
