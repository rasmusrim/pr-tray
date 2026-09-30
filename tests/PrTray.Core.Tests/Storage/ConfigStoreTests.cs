using PrTray.Core.Presentation;
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

        Assert.Equal(PrTrayConfig.Default.Repositories, config.Repositories);
        Assert.Equal(120, config.PollIntervalSeconds);
        Assert.Equal("gh", config.GhPath);
        Assert.Equal(NotificationSound.Message, config.NotificationSound);
        Assert.Null(config.CustomSoundFile);
        Assert.Equal(TextSize.Normal, config.TextSize);
        Assert.Contains("\"repositories\"", File.ReadAllText(ConfigPath));
    }

    [Fact]
    public void Stored_values_are_loaded()
    {
        var config = LoadFrom("""{ "repositories": ["rasmusrim/ku"], "pollIntervalSeconds": 300, "ghPath": "/opt/homebrew/bin/gh", "notificationSound": "bell" }""");

        Assert.Equal(new[] { "rasmusrim/ku" }, config.Repositories);
        Assert.Equal(300, config.PollIntervalSeconds);
        Assert.Equal("/opt/homebrew/bin/gh", config.GhPath);
        Assert.Equal(NotificationSound.Bell, config.NotificationSound);
    }

    [Fact]
    public void Invalid_repository_names_are_dropped()
    {
        var config = LoadFrom("""{ "repositories": ["acme/widgets", "foo bar/baz", "x\" is:closed", "justname", null, "ACME/widgets"] }""");

        Assert.Equal(new[] { "acme/widgets" }, config.Repositories);
    }

    [Fact]
    public void Too_short_poll_interval_is_clamped_and_missing_values_get_defaults()
    {
        var config = LoadFrom("""{ "pollIntervalSeconds": 5 }""");

        Assert.Equal(ConfigStore.MinimumPollIntervalSeconds, config.PollIntervalSeconds);
        Assert.Empty(config.Repositories);
        Assert.Equal("gh", config.GhPath);
        Assert.Equal(NotificationSound.Message, config.NotificationSound);
    }

    [Fact]
    public void Corrupt_file_falls_back_to_defaults_without_overwriting_it()
    {
        var config = LoadFrom("{ not json");

        Assert.Equal(PrTrayConfig.Default.Repositories, config.Repositories);
        Assert.Equal("{ not json", File.ReadAllText(ConfigPath));
    }

    [Fact]
    public void Legacy_watched_repositories_key_is_still_read()
    {
        var config = LoadFrom("""{ "watchedRepositories": ["rasmusrim/ku"] }""");

        Assert.Equal(new[] { "rasmusrim/ku" }, config.Repositories);
    }

    [Fact]
    public void Saved_config_round_trips_under_the_new_key()
    {
        var store = new ConfigStore(ConfigPath);
        var config = new PrTrayConfig(["rasmusrim/ku", "acme/widgets"], 300, "gh", NotificationSound.Custom, "/home/me/pling.wav", TextSize.Larger);

        store.Save(config);

        Assert.Equal(config.Repositories, store.LoadOrCreate().Repositories);
        Assert.Equal(300, store.LoadOrCreate().PollIntervalSeconds);
        Assert.Equal(NotificationSound.Custom, store.LoadOrCreate().NotificationSound);
        Assert.Equal("/home/me/pling.wav", store.LoadOrCreate().CustomSoundFile);
        Assert.Equal(TextSize.Larger, store.LoadOrCreate().TextSize);
        Assert.Contains("\"textSize\": \"larger\"", File.ReadAllText(ConfigPath));
        Assert.Contains("\"notificationSound\": \"custom\"", File.ReadAllText(ConfigPath));
        Assert.DoesNotContain("watchedRepositories", File.ReadAllText(ConfigPath));
    }

    [Theory]
    [InlineData("""{ "notificationSound": "trumpet" }""")]
    [InlineData("""{ "notificationSound": "custom" }""")]
    [InlineData("""{ "notificationSound": "custom", "customSoundFile": "  " }""")]
    public void Unknown_sound_or_custom_sound_without_file_falls_back_to_default(string json)
    {
        Assert.Equal(PrTrayConfig.Default.NotificationSound, LoadFrom(json).NotificationSound);
    }

    [Fact]
    public void Sound_can_be_turned_off()
    {
        Assert.Equal(NotificationSound.None, LoadFrom("""{ "notificationSound": "none" }""").NotificationSound);
    }

    [Theory]
    [InlineData("""{ "textSize": "large" }""", TextSize.Large)]
    [InlineData("""{ "textSize": "LARGEST" }""", TextSize.Largest)]
    [InlineData("""{ "textSize": "huge" }""", TextSize.Normal)]
    [InlineData("""{ "textSize": "7" }""", TextSize.Normal)]
    [InlineData("""{ }""", TextSize.Normal)]
    public void Text_size_is_loaded_and_unknown_values_fall_back_to_normal(string json, TextSize expected)
    {
        Assert.Equal(expected, LoadFrom(json).TextSize);
    }

    [Theory]
    [InlineData("acme/widgets", true)]
    [InlineData("rasmusrim/Shopify2Fiken", true)]
    [InlineData("justname", false)]
    [InlineData("foo bar/baz", false)]
    [InlineData("owner/repo\" is:closed", false)]
    [InlineData("", false)]
    public void Repository_name_validation(string repository, bool expected)
    {
        Assert.Equal(expected, ConfigStore.IsValidRepositoryName(repository));
    }

    [Fact]
    public void Default_config_has_no_repository_filter()
    {
        Assert.Empty(PrTrayConfig.Default.Repositories);
    }
}
