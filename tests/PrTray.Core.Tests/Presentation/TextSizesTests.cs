using PrTray.Core.Presentation;

namespace PrTray.Core.Tests.Presentation;

public class TextSizesTests
{
    [Fact]
    public void Every_size_has_a_label_and_sizes_grow_in_order()
    {
        var sizes = Enum.GetValues<TextSize>();
        foreach (var size in sizes)
            Assert.False(string.IsNullOrWhiteSpace(TextSizes.Label(size)));
        Assert.Equal(sizes.Select(TextSizes.Scale).Order(), sizes.Select(TextSizes.Scale));
        Assert.Equal(1.0, TextSizes.Scale(TextSize.Normal));
    }

    [Fact]
    public void Labels_are_norwegian_and_show_the_percentage()
    {
        Assert.Equal("Normal (100 %)", TextSizes.Label(TextSize.Normal));
        Assert.Equal("Størst (150 %)", TextSizes.Label(TextSize.Largest));
    }
}
