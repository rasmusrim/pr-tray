namespace PrTray.Core.Tests;

internal sealed class TemporaryDirectory : IDisposable
{
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "prtray-tests-" + Guid.NewGuid().ToString("N"));

    public string FilePath(string fileName) => Path.Combine(DirectoryPath, fileName);

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}
