using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.UnitTests.Catalog;

public class IsbnTests
{
    [Theory]
    [InlineData("9780306406157", "9780306406157")]
    [InlineData("978-0-306-40615-7", "9780306406157")]
    [InlineData("978 0 306 40615 7", "9780306406157")]
    [InlineData("0-306-40615-2", "9780306406157")] // ISBN-10 converts to 13
    [InlineData("0306406152", "9780306406157")]
    [InlineData("080442957X", "9780804429573")] // X check digit
    public void TryNormalize_returns_canonical_isbn13_for_valid_input(string input, string expected)
    {
        Assert.Equal(expected, Isbn.TryNormalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-isbn")]
    [InlineData("9780306406158")] // bad check digit
    [InlineData("0306406153")] // bad check digit
    [InlineData("123456789")] // too short
    [InlineData("97803064061571")] // too long
    [InlineData("030640615X2")] // X not in final position
    public void TryNormalize_returns_null_for_invalid_input(string? input)
    {
        Assert.Null(Isbn.TryNormalize(input));
    }

    [Fact]
    public void IsValid_matches_TryNormalize()
    {
        Assert.True(Isbn.IsValid("978-0-306-40615-7"));
        Assert.False(Isbn.IsValid("garbage"));
    }
}
