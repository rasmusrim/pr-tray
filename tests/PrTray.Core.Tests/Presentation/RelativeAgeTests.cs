using PrTray.Core.Presentation;

namespace PrTray.Core.Tests.Presentation;

public class RelativeAgeTests
{
    [Theory]
    [InlineData(-5, "nå")]
    [InlineData(30, "nå")]
    [InlineData(12 * 60, "12 min")]
    [InlineData(3 * 3600 + 59, "3 t")]
    [InlineData(5 * 86400, "5 d")]
    public void Formats_age(int seconds, string expected)
    {
        Assert.Equal(expected, RelativeAge.Format(TimeSpan.FromSeconds(seconds)));
    }
}
