using PrTray.Core.GitHub;

namespace PrTray.Core.Tests.GitHub;

public class GhQueryBuilderTests
{
    private static readonly DateOnly MergedSince = new(2026, 9, 21);

    [Fact]
    public void Query_without_watched_repositories_has_no_watched_aliases()
    {
        var query = GhQueryBuilder.Build([], MergedSince);

        Assert.Contains("viewer { login }", query);
        Assert.Contains("mine: search(query: \"is:pr is:open archived:false author:@me\"", query);
        Assert.Contains("requested: search(query: \"is:pr is:open archived:false review-requested:@me\"", query);
        Assert.Contains("reviewed: search(query: \"is:pr is:open archived:false reviewed-by:@me\"", query);
        Assert.Contains("mergedMine: search(query: \"is:pr is:merged merged:>=2026-09-21 author:@me\"", query);
        Assert.Contains("mergedReviewed: search(query: \"is:pr is:merged merged:>=2026-09-21 reviewed-by:@me\"", query);
        Assert.DoesNotContain("watched:", query);
        Assert.DoesNotContain("mergedWatched:", query);
        Assert.Contains("fragment PrFields on PullRequest", query);
    }

    [Fact]
    public void Query_with_watched_repositories_lists_each_repo()
    {
        var query = GhQueryBuilder.Build(["acme/widgets", "rasmusrim/ku"], MergedSince);

        Assert.Contains("watched: search(query: \"is:pr is:open archived:false repo:acme/widgets repo:rasmusrim/ku\"", query);
        Assert.Contains("mergedWatched: search(query: \"is:pr is:merged merged:>=2026-09-21 repo:acme/widgets repo:rasmusrim/ku\"", query);
    }

    [Fact]
    public void Every_alias_in_the_query_has_a_group_mapping()
    {
        var query = GhQueryBuilder.Build(["acme/widgets"], MergedSince);

        foreach (var alias in GhQueryBuilder.GroupByAlias.Keys)
            Assert.Contains($"{alias}: search(", query);
    }

    [Fact]
    public void Repository_filter_is_applied_to_every_search()
    {
        var query = GhQueryBuilder.Build(["acme/widgets"], MergedSince);

        Assert.Contains("mine: search(query: \"is:pr is:open archived:false author:@me repo:acme/widgets\"", query);
        Assert.Contains("requested: search(query: \"is:pr is:open archived:false review-requested:@me repo:acme/widgets\"", query);
        Assert.Contains("reviewed: search(query: \"is:pr is:open archived:false reviewed-by:@me repo:acme/widgets\"", query);
        Assert.Contains("mergedMine: search(query: \"is:pr is:merged merged:>=2026-09-21 author:@me repo:acme/widgets\"", query);
        Assert.Contains("mergedReviewed: search(query: \"is:pr is:merged merged:>=2026-09-21 reviewed-by:@me repo:acme/widgets\"", query);
    }
}
