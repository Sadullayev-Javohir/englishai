using FluentAssertions;
using Xunit;

namespace Integration.Tests;

public sealed class ContentSecurityPolicyTests
{
    [Fact]
    public async Task Google_font_hosts_are_allowed_only_by_their_required_directives()
    {
        var caddyfile = await File.ReadAllTextAsync(FindCaddyfile());

        Directive(caddyfile, "style-src").Should().Contain("https://fonts.googleapis.com");
        Directive(caddyfile, "font-src").Should().Contain("https://fonts.gstatic.com");
        Directive(caddyfile, "style-src").Split(' ').Should().NotContain("https:");
        Directive(caddyfile, "font-src").Split(' ').Should().NotContain("https:");
    }

    private static string FindCaddyfile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "Caddyfile");
            if (File.Exists(path))
                return path;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find the repository Caddyfile.");
    }

    private static string Directive(string caddyfile, string name)
    {
        var start = caddyfile.IndexOf($"{name} ", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var end = caddyfile.IndexOf(';', start);
        end.Should().BeGreaterThan(start);
        return caddyfile[start..end];
    }
}
