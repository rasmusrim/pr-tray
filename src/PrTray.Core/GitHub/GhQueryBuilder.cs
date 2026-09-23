using PrTray.Core.Models;

namespace PrTray.Core.GitHub;

public static class GhQueryBuilder
{
    public static readonly IReadOnlyDictionary<string, PrGroups> GroupByAlias = new Dictionary<string, PrGroups>
    {
        ["mine"] = PrGroups.Mine,
        ["requested"] = PrGroups.ReviewRequested,
        ["reviewed"] = PrGroups.ReviewedByMe,
        ["watched"] = PrGroups.Watched,
        ["mergedMine"] = PrGroups.Mine,
        ["mergedReviewed"] = PrGroups.ReviewedByMe,
        ["mergedWatched"] = PrGroups.Watched,
    };

    private const int MaxResultsPerSearch = 50;

    private const string OpenFilter = "is:pr is:open archived:false";

    private const string PrFieldsFragment =
        "fragment PrFields on PullRequest { id number title url state isDraft createdAt mergedAt " +
        "author { login } repository { nameWithOwner } reviewDecision " +
        "commits(last: 1) { nodes { commit { oid committedDate } } } " +
        "reviews(last: 30) { nodes { id state submittedAt author { __typename login } commit { oid } } } }";

    public static string Build(IReadOnlyList<string> repositories, DateOnly mergedSince)
    {
        var repositoryFilter = string.Join(" ", repositories.Select(repository => $"repo:{repository}"));
        var mergedFilter = $"is:pr is:merged merged:>={mergedSince:yyyy-MM-dd}";
        var searches = new List<string>
        {
            Search("mine", $"{OpenFilter} author:@me", repositoryFilter),
            Search("requested", $"{OpenFilter} review-requested:@me", repositoryFilter),
            Search("reviewed", $"{OpenFilter} reviewed-by:@me", repositoryFilter),
            Search("mergedMine", $"{mergedFilter} author:@me", repositoryFilter),
            Search("mergedReviewed", $"{mergedFilter} reviewed-by:@me", repositoryFilter),
        };
        if (repositories.Count > 0)
        {
            searches.Add(Search("watched", OpenFilter, repositoryFilter));
            searches.Add(Search("mergedWatched", mergedFilter, repositoryFilter));
        }
        return $"query {{ viewer {{ login }} {string.Join(" ", searches)} }} {PrFieldsFragment}";
    }

    private static string Search(string alias, string searchQuery, string repositoryFilter)
    {
        var filteredQuery = repositoryFilter.Length == 0 ? searchQuery : $"{searchQuery} {repositoryFilter}";
        return $"{alias}: search(query: \"{filteredQuery}\", type: ISSUE, first: {MaxResultsPerSearch}) {{ nodes {{ ...PrFields }} }}";
    }
}
