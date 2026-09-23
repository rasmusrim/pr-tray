using PrTray.Core.Presentation;

namespace PrTray.Core.Tests.Presentation;

public class RepositoryInputTests
{
    private static readonly string[] Existing = ["acme/widgets"];

    [Fact]
    public void Valid_new_repository_has_no_problem()
    {
        Assert.Null(RepositoryInput.Problem(Existing, "rasmusrim/ku"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("widgets")]
    [InlineData("acme corp/widgets")]
    public void Malformed_name_explains_the_format(string candidate)
    {
        Assert.Equal("Skriv repoet som eier/repo, f.eks. acme/widgets", RepositoryInput.Problem(Existing, candidate));
    }

    [Fact]
    public void Duplicate_is_rejected_regardless_of_case()
    {
        Assert.Equal("Repoet er allerede i listen", RepositoryInput.Problem(Existing, "Acme/Widgets"));
    }

    [Fact]
    public void Github_url_is_normalized_to_owner_and_repository()
    {
        Assert.Equal("rasmusrim/ku", RepositoryInput.Normalize(" https://github.com/rasmusrim/ku/pulls "));
        Assert.Equal("rasmusrim/ku", RepositoryInput.Normalize("rasmusrim/ku"));
    }
}
