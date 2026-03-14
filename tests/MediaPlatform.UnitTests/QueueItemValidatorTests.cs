using FluentAssertions;
using MediaPlatform.Application.Validation;
using Xunit;

namespace MediaPlatform.UnitTests;

public class QueueItemValidatorTests
{
    // --- URL required ---

    [Fact]
    public void Validate_NullUrl_Fails()
    {
        var result = QueueItemValidator.Validate(null, "Title");
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("URL is required");
    }

    [Fact]
    public void Validate_EmptyUrl_Fails()
    {
        var result = QueueItemValidator.Validate("", "Title");
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("URL is required");
    }

    [Fact]
    public void Validate_WhitespaceUrl_Fails()
    {
        var result = QueueItemValidator.Validate("   ", "Title");
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("URL is required");
    }

    // --- URL max length ---

    [Fact]
    public void Validate_UrlExceeds2048_Fails()
    {
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&" + new string('x', 2048);
        var result = QueueItemValidator.Validate(url, null);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("URL exceeds maximum length");
    }

    // --- URL format ---

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("javascript:alert(1)")]
    public void Validate_InvalidUrlFormat_Fails(string url)
    {
        var result = QueueItemValidator.Validate(url, null);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Invalid URL format");
    }

    // --- Non-YouTube URLs ---

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("https://vimeo.com/12345678")]
    [InlineData("https://www.dailymotion.com/video/x7tgad")]
    public void Validate_NonYouTubeUrl_Fails(string url)
    {
        var result = QueueItemValidator.Validate(url, null);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Only YouTube URLs are supported");
    }

    // --- Title max length ---

    [Fact]
    public void Validate_TitleExceeds200_Fails()
    {
        var title = new string('a', 201);
        var result = QueueItemValidator.Validate("https://www.youtube.com/watch?v=dQw4w9WgXcQ", title);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Title exceeds 200 characters");
    }

    [Fact]
    public void Validate_TitleExactly200_Succeeds()
    {
        var title = new string('a', 200);
        var result = QueueItemValidator.Validate("https://www.youtube.com/watch?v=dQw4w9WgXcQ", title);
        result.IsValid.Should().BeTrue();
    }

    // --- Valid YouTube URLs ---

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://music.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf", "dQw4w9WgXcQ")]
    public void Validate_ValidYouTubeUrl_Succeeds(string url, string expectedVideoId)
    {
        var result = QueueItemValidator.Validate(url, null);
        result.IsValid.Should().BeTrue();
        result.VideoId.Should().Be(expectedVideoId);
    }

    [Fact]
    public void Validate_NullTitle_Succeeds()
    {
        var result = QueueItemValidator.Validate("https://www.youtube.com/watch?v=dQw4w9WgXcQ", null);
        result.IsValid.Should().BeTrue();
    }
}

public class QueueItemVideoIdExtractionTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://music.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=120", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?list=PLrAXtmErZg&v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void ExtractVideoId_ValidUrl_ReturnsId(string url, string expected)
    {
        QueueItemValidator.ExtractVideoId(url).Should().Be(expected);
    }

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("https://vimeo.com/12345")]
    [InlineData("not-a-url")]
    public void ExtractVideoId_InvalidUrl_ReturnsNull(string url)
    {
        QueueItemValidator.ExtractVideoId(url).Should().BeNull();
    }
}
