using System.Text.Json;
using PrTray.Core.Presentation;

namespace PrTray.Core.Storage;

public sealed class UnreadCommentsStore(string filePath)
{
    public UnreadComments Load()
    {
        try
        {
            var entries = JsonSerializer.Deserialize<List<UnreadComment?>>(File.ReadAllText(filePath)) ?? [];
            return new UnreadComments(entries
                .OfType<UnreadComment>()
                .Where(entry => entry is { PullRequestId: not null, PullRequestUrl: not null, FirstCommentUrl: not null })
                .ToList());
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException or JsonException)
        {
            return UnreadComments.None;
        }
    }

    public void Save(UnreadComments unreadComments)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var temporaryPath = filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(unreadComments.Entries));
        File.Move(temporaryPath, filePath, overwrite: true);
    }
}
