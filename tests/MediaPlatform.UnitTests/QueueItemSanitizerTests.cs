using FluentAssertions;
using MediaPlatform.Application.Validation;
using Xunit;

namespace MediaPlatform.UnitTests;

public class QueueItemSanitizerTests
{
    [Fact]
    public void Sanitize_TrimsUrl()
    {
        var (url, _) = QueueItemSanitizer.Sanitize("  https://youtube.com/watch?v=abc  ", null);
        url.Should().Be("https://youtube.com/watch?v=abc");
    }

    [Fact]
    public void Sanitize_TrimsTitle()
    {
        var (_, title) = QueueItemSanitizer.Sanitize("https://youtube.com", "  My Video  ");
        title.Should().Be("My Video");
    }

    [Fact]
    public void Sanitize_NullTitle_ReturnsNull()
    {
        var (_, title) = QueueItemSanitizer.Sanitize("https://youtube.com", null);
        title.Should().BeNull();
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>My Video", "alert('xss')My Video")]
    [InlineData("<b>Bold</b> text", "Bold text")]
    [InlineData("Normal title", "Normal title")]
    [InlineData("<img src=x onerror=alert(1)>Title", "Title")]
    [InlineData("<div><span>Nested</span></div> text", "Nested text")]
    public void Sanitize_StripsHtmlFromTitle(string input, string expected)
    {
        var (_, title) = QueueItemSanitizer.Sanitize("https://youtube.com", input);
        title.Should().Be(expected);
    }
}
