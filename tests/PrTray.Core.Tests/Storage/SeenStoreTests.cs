using PrTray.Core.Storage;

namespace PrTray.Core.Tests.Storage;

public sealed class SeenStoreTests : IDisposable
{
    private readonly TemporaryDirectory temporaryDirectory = new();

    private SeenStore Store => new(temporaryDirectory.FilePath("seen.json"));

    public void Dispose() => temporaryDirectory.Dispose();

    [Fact]
    public void Missing_file_is_first_run()
    {
        var seen = Store.Load();

        Assert.True(seen.IsFirstRun);
        Assert.Empty(seen.Keys);
    }

    [Fact]
    public void Saved_keys_round_trip_and_are_no_longer_first_run()
    {
        Store.Save(new HashSet<string> { "review:R_1", "opened:PR_1" });

        var seen = Store.Load();

        Assert.False(seen.IsFirstRun);
        Assert.Equal(new[] { "opened:PR_1", "review:R_1" }, seen.Keys.Order());
    }

    [Fact]
    public void Saving_an_empty_set_still_ends_first_run()
    {
        Store.Save(new HashSet<string>());

        Assert.False(Store.Load().IsFirstRun);
    }

    [Fact]
    public void Corrupt_file_is_treated_as_first_run()
    {
        Directory.CreateDirectory(temporaryDirectory.DirectoryPath);
        File.WriteAllText(temporaryDirectory.FilePath("seen.json"), "[oops");

        Assert.True(Store.Load().IsFirstRun);
    }
}
