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

    public static string Build(IReadOnlyList<string> watchedRepositories, DateOnly mergedSince)
    {
        var mergedFilter = $"is:pr is:merged merged:>={mergedSince:yyyy-MM-dd}";
        var searches = new List<string>
        {
            Search("mine", $"{OpenFilter} author:@me"),
            Search("requested", $"{OpenFilter} review-requested:@me"),
            Search("reviewed", $"{OpenFilter} reviewed-by:@me"),
            Search("mergedMine", $"{mergedFilter} author:@me"),
            Search("mergedReviewed", $"{mergedFilter} reviewed-by:@me"),
        };
        if (watchedRepositories.Count > 0)
        {
            var repositoryFilter = string.Join(" ", watchedRepositories.Select(repository => $"repo:{repository}"));
            searches.Add(Search("watched", $"{OpenFilter} {repositoryFilter}"));
            searches.Add(Search("mergedWatched", $"{mergedFilter} {repositoryFilter}"));
        }
        return $"query {{ viewer {{ login }} {string.Join(" ", searches)} }} {PrFieldsFragment}";
    }

    private static string Search(string alias, string searchQuery) =>
        $"{alias}: search(query: \"{searchQuery}\", type: ISSUE, first: {MaxResultsPerSearch}) {{ nodes {{ ...PrFields }} }}";
}
