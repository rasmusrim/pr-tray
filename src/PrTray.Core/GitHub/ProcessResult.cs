namespace PrTray.Core.GitHub;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
