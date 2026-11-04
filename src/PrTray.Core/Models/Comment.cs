namespace PrTray.Core.Models;

public sealed record Comment(string Id, string AuthorLogin, bool AuthorIsBot, DateTimeOffset? CreatedAt, string Url, string BodyText);
