using FluentAssertions;
using MediaPlatform.Domain.ValueObjects;
using Xunit;

namespace MediaPlatform.UnitTests;

public class VideoUrlTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    public void Create_ValidYouTubeUrl_Succeeds(string url)
    {
        var videoUrl = VideoUrl.Create(url);

        videoUrl.Value.Should().StartWith("https://");
    }

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("https://vimeo.com/12345")]
    [InlineData("https://evil.com/youtube.com")]
    public void Create_NonYouTubeUrl_Throws(string url)
    {
        var act = () => VideoUrl.Create(url);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*YouTube*");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("")]
    public void Create_InvalidFormat_Throws(string url)
    {
        var act = () => VideoUrl.Create(url);

        act.Should().Throw<ArgumentException>();
    }
}
