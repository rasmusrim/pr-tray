namespace PrTray.Core.Storage;

public static class AppPaths
{
    private const string AppFolderName = "PrTray";

    public static string ConfigFile => Path.Combine(ConfigDirectory, "config.json");

    public static string SeenFile => Path.Combine(StateDirectory, "seen.json");

    private static string ConfigDirectory =>
        OperatingSystem.IsWindows() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName)
        : OperatingSystem.IsMacOS() ? MacDirectory
        : Path.Combine(XdgDirectory("XDG_CONFIG_HOME", ".config"), AppFolderName);

    private static string StateDirectory =>
        OperatingSystem.IsWindows() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName)
        : OperatingSystem.IsMacOS() ? MacDirectory
        : Path.Combine(XdgDirectory("XDG_STATE_HOME", Path.Combine(".local", "state")), AppFolderName);

    private static string MacDirectory => Path.Combine(HomeDirectory, "Library", "Application Support", AppFolderName);

    private static string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private static string XdgDirectory(string environmentVariable, string fallbackRelativeToHome)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(value) ? Path.Combine(HomeDirectory, fallbackRelativeToHome) : value;
    }
}
