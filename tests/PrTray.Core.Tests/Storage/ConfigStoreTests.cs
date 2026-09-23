using PrTray.Core.Storage;

namespace PrTray.Core.Tests.Storage;

public sealed class ConfigStoreTests : IDisposable
{
    private readonly TemporaryDirectory temporaryDirectory = new();

    private string ConfigPath => temporaryDirectory.FilePath("config.json");

    public void Dispose() => temporaryDirectory.Dispose();

    private PrTrayConfig LoadFrom(string json)
    {
        Directory.CreateDirectory(temporaryDirectory.DirectoryPath);
        File.WriteAllText(ConfigPath, json);
        return new ConfigStore(ConfigPath).LoadOrCreate();
    }

    [Fact]
    public void Missing_file_is_created_with_defaults()
    {
        var config = new ConfigStore(ConfigPath).LoadOrCreate();

        Assert.Equal(PrTrayConfig.Default.WatchedRepositories, config.WatchedRepositories);
        Assert.Equal(120, config.PollIntervalSeconds);
        Assert.Equal("gh", config.GhPath);
        Assert.Contains("\"watchedRepositories\"", File.ReadAllText(ConfigPath));
    }

    [Fact]
    public void Stored_values_are_loaded()
    {
        var config = LoadFrom("""{ "watchedRepositories": ["rasmusrim/ku"], "pollIntervalSeconds": 300, "ghPath": "/opt/homebrew/bin/gh" }""");

        Assert.Equal(new[] { "rasmusrim/ku" }, config.WatchedRepositories);
        Assert.Equal(300, config.PollIntervalSeconds);
        Assert.Equal("/opt/homebrew/bin/gh", config.GhPath);
    }

    [Fact]
    public void Invalid_repository_names_are_dropped()
    {
        var config = LoadFrom("""{ "watchedRepositories": ["acme/widgets", "foo bar/baz", "x\" is:closed", "justname", null, "ACME/widgets"] }""");

        Assert.Equal(new[] { "acme/widgets" }, config.WatchedRepositories);
    }

    [Fact]
    public void Too_short_poll_interval_is_clamped_and_missing_values_get_defaults()
    {
        var config = LoadFrom("""{ "pollIntervalSeconds": 5 }""");

        Assert.Equal(ConfigStore.MinimumPollIntervalSeconds, config.PollIntervalSeconds);
        Assert.Empty(config.WatchedRepositories);
        Assert.Equal("gh", config.GhPath);
    }

    [Fact]
    public void Corrupt_file_falls_back_to_defaults_without_overwriting_it()
    {
        var config = LoadFrom("{ not json");

        Assert.Equal(PrTrayConfig.Default.WatchedRepositories, config.WatchedRepositories);
        Assert.Equal("{ not json", File.ReadAllText(ConfigPath));
    }
}
