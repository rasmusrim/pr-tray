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
    [InlineData(45 * 86400, "1 mnd")]
    [InlineData(209 * 86400, "6 mnd")]
    [InlineData(2108 * 86400, "5 år")]
    public void Formats_age(int seconds, string expected)
    {
        Assert.Equal(expected, RelativeAge.Format(TimeSpan.FromSeconds(seconds)));
    }
}
