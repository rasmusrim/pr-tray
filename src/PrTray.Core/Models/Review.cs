namespace PrTray.Core.Models;

public sealed record Review(
    string Id,
    ReviewState State,
    string AuthorLogin,
    bool AuthorIsBot,
    DateTimeOffset? SubmittedAt,
    string? CommitOid,
    string Url,
    string BodyText)
{
    public Comment AsComment() => new(Id, AuthorLogin, AuthorIsBot, SubmittedAt, Url, BodyText);
}
