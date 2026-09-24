using System.ComponentModel;
using System.Text.Json;
using PrTray.Core.Storage;

namespace PrTray.Core.GitHub;

public sealed class GhClient(IProcessRunner processRunner, TimeProvider timeProvider, TimeSpan? timeout = null)
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    private const int GhAuthRequiredExitCode = 4;
    private const int MergedLookbackDays = 2;

    public async Task<GhResult> FetchAsync(PrTrayConfig config, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var mergedSince = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-MergedLookbackDays);
        var query = GhQueryBuilder.Build(config.Repositories, mergedSince);
        var effectiveTimeout = timeout ?? DefaultTimeout;
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(effectiveTimeout);

        ProcessResult result;
        try
        {
            result = await processRunner.RunAsync(config.GhPath, ["api", "graphql", "-f", $"query={query}"], timeoutSource.Token);
        }
        catch (Win32Exception)
        {
            return new GhResult.NotInstalled();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new GhResult.Failed($"gh svarte ikke innen {(int)effectiveTimeout.TotalSeconds} s");
        }

        if (result.ExitCode == GhAuthRequiredExitCode)
            return new GhResult.NotAuthenticated();
        if (result.ExitCode != 0)
            return new GhResult.Failed(FirstLine(result.StandardError));

        try
        {
            return new GhResult.Success(GhResponseParser.Parse(result.StandardOutput, now));
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return new GhResult.Failed($"Uventet svar fra gh: {exception.Message}");
        }
    }

    public async Task<RepositoryCheck> CheckRepositoryAsync(PrTrayConfig config, string repository, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout ?? DefaultTimeout);
        try
        {
            var result = await processRunner.RunAsync(config.GhPath, ["api", $"repos/{repository}", "--silent"], timeoutSource.Token);
            if (result.ExitCode == 0)
                return RepositoryCheck.Exists;
            return result.StandardError.Contains("HTTP 404") ? RepositoryCheck.NotFound : RepositoryCheck.Unknown;
        }
        catch (Exception exception) when (exception is Win32Exception || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return RepositoryCheck.Unknown;
        }
    }

    private static string FirstLine(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "gh feilet uten melding";
}
