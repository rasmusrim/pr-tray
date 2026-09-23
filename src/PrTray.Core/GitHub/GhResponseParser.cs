using System.Text.Json;
using PrTray.Core.Models;

namespace PrTray.Core.GitHub;

public static class GhResponseParser
{
    private const string GhostLogin = "ghost";

    public static PrSnapshot Parse(string json, DateTimeOffset fetchedAt)
    {
        using var document = JsonDocument.Parse(json);
        var data = document.RootElement.GetProperty("data");
        var viewerLogin = data.GetProperty("viewer").GetProperty("login").GetString() ?? "";
        var pullRequestsById = new Dictionary<string, PullRequest>();
        foreach (var aliasGroup in GhQueryBuilder.GroupByAlias)
        {
            if (!data.TryGetProperty(aliasGroup.Key, out var search) || search.ValueKind != JsonValueKind.Object)
                continue;
            foreach (var node in search.GetProperty("nodes").EnumerateArray())
            {
                if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty("id", out _))
                    continue;
                var pullRequest = ParsePullRequest(node, aliasGroup.Value);
                pullRequestsById[pullRequest.Id] = pullRequestsById.TryGetValue(pullRequest.Id, out var existing)
                    ? existing with { Groups = existing.Groups | pullRequest.Groups }
                    : pullRequest;
            }
        }
        return new PrSnapshot(viewerLogin, pullRequestsById.Values.ToList(), fetchedAt);
    }

    private static PullRequest ParsePullRequest(JsonElement node, PrGroups group)
    {
        var headCommit = node.GetProperty("commits").GetProperty("nodes").EnumerateArray()
            .Select(commitNode => commitNode.GetProperty("commit"))
            .FirstOrDefault();
        var hasHeadCommit = headCommit.ValueKind == JsonValueKind.Object;
        return new PullRequest(
            Id: node.GetProperty("id").GetString()!,
            Repository: node.GetProperty("repository").GetProperty("nameWithOwner").GetString()!,
            Number: node.GetProperty("number").GetInt32(),
            Title: node.GetProperty("title").GetString() ?? "",
            Url: node.GetProperty("url").GetString()!,
            State: ParseState(OptionalString(node, "state")),
            IsDraft: node.GetProperty("isDraft").GetBoolean(),
            AuthorLogin: LoginOf(node),
            CreatedAt: node.GetProperty("createdAt").GetDateTimeOffset(),
            MergedAt: OptionalDate(node, "mergedAt"),
            ReviewDecision: ParseReviewDecision(OptionalString(node, "reviewDecision")),
            HeadCommitOid: hasHeadCommit ? OptionalString(headCommit, "oid") : null,
            HeadCommittedAt: hasHeadCommit ? OptionalDate(headCommit, "committedDate") : null,
            Reviews: node.GetProperty("reviews").GetProperty("nodes").EnumerateArray().Select(ParseReview).ToList(),
            Groups: group);
    }

    private static Review ParseReview(JsonElement node) => new(
        Id: node.GetProperty("id").GetString()!,
        State: ParseReviewState(OptionalString(node, "state")),
        AuthorLogin: LoginOf(node),
        AuthorIsBot: node.TryGetProperty("author", out var author) && author.ValueKind == JsonValueKind.Object
            && OptionalString(author, "__typename") == "Bot",
        SubmittedAt: OptionalDate(node, "submittedAt"),
        CommitOid: node.TryGetProperty("commit", out var commit) && commit.ValueKind == JsonValueKind.Object
            ? OptionalString(commit, "oid")
            : null);

    private static string LoginOf(JsonElement node) =>
        node.TryGetProperty("author", out var author) && author.ValueKind == JsonValueKind.Object
            ? OptionalString(author, "login") ?? GhostLogin
            : GhostLogin;

    private static string? OptionalString(JsonElement node, string propertyName) =>
        node.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static DateTimeOffset? OptionalDate(JsonElement node, string propertyName) =>
        node.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetDateTimeOffset() : null;

    private static PrState ParseState(string? value) => value switch
    {
        "MERGED" => PrState.Merged,
        "CLOSED" => PrState.Closed,
        _ => PrState.Open,
    };

    private static ReviewState ParseReviewState(string? value) => value switch
    {
        "APPROVED" => ReviewState.Approved,
        "CHANGES_REQUESTED" => ReviewState.ChangesRequested,
        "DISMISSED" => ReviewState.Dismissed,
        "PENDING" => ReviewState.Pending,
        _ => ReviewState.Commented,
    };

    private static ReviewDecision ParseReviewDecision(string? value) => value switch
    {
        "APPROVED" => ReviewDecision.Approved,
        "CHANGES_REQUESTED" => ReviewDecision.ChangesRequested,
        "REVIEW_REQUIRED" => ReviewDecision.ReviewRequired,
        _ => ReviewDecision.None,
    };
}
