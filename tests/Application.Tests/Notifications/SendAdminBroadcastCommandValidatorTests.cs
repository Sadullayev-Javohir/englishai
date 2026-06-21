using Application.Notifications.SendAdminBroadcast;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Notifications;

public class SendAdminBroadcastCommandValidatorTests
{
    private static readonly Guid Author = Guid.NewGuid();
    private readonly SendAdminBroadcastCommandValidator _validator = new();

    private static SendAdminBroadcastCommand Command(string? linkUrl) =>
        new(Author, "Sarlavha", "Matn", linkUrl);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/home")]
    [InlineData("/speaking")]
    [InlineData("https://englishai.uz/promo")]
    [InlineData("http://example.com/path?x=1")]
    public void Accepts_no_link_known_route_or_absolute_http_link(string? linkUrl)
    {
        _validator.TestValidate(Command(linkUrl)).ShouldNotHaveValidationErrorFor(x => x.LinkUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]     // dangerous scheme
    [InlineData("mailto:a@b.com")]           // non-http scheme
    [InlineData("/unknown-internal-route")]  // not a known in-app route, not absolute
    [InlineData("englishai.uz")]             // missing scheme → not absolute
    public void Rejects_dangerous_or_malformed_destinations(string linkUrl)
    {
        _validator.TestValidate(Command(linkUrl)).ShouldHaveValidationErrorFor(x => x.LinkUrl);
    }

    [Fact]
    public void Rejects_a_link_longer_than_the_column()
    {
        var tooLong = "https://englishai.uz/" + new string('a', SendAdminBroadcastCommandValidator.MaxLinkUrlLength);
        _validator.TestValidate(Command(tooLong)).ShouldHaveValidationErrorFor(x => x.LinkUrl);
    }
}
