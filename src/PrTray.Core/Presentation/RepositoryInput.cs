using PrTray.Core.Storage;

namespace PrTray.Core.Presentation;

public static class RepositoryInput
{
    private const string GithubUrlPrefix = "https://github.com/";

    public static string Normalize(string input)
    {
        var trimmed = input.Trim();
        if (!trimmed.StartsWith(GithubUrlPrefix, StringComparison.OrdinalIgnoreCase))
            return trimmed;
        var pathSegments = trimmed[GithubUrlPrefix.Length..].Split('/', StringSplitOptions.RemoveEmptyEntries);
        return pathSegments.Length >= 2 ? $"{pathSegments[0]}/{pathSegments[1]}" : trimmed;
    }

    public static string? Problem(IReadOnlyList<string> existingRepositories, string candidate)
    {
        if (!ConfigStore.IsValidRepositoryName(candidate))
            return "Skriv repoet som eier/repo, f.eks. acme/widgets";
        if (existingRepositories.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            return "Repoet er allerede i listen";
        return null;
    }
}
