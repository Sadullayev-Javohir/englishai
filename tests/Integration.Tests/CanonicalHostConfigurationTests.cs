using FluentAssertions;

namespace Integration.Tests;

public sealed class CanonicalHostConfigurationTests
{
    [Fact]
    public async Task Caddy_redirects_http_and_www_hosts_to_canonical_apex_https_in_one_hop()
    {
        var caddyfile = await File.ReadAllTextAsync(FindCaddyfile());

        caddyfile.Should().Contain("http://{$DOMAIN}, http://www.{$DOMAIN}, https://www.{$DOMAIN}");
        caddyfile.Should().Contain("redir https://{$DOMAIN}{uri} permanent");
        caddyfile.Should().Contain("https://{$DOMAIN} {");
    }

    private static string FindCaddyfile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "Caddyfile");
            if (File.Exists(path)) return path;
            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find the repository Caddyfile.");
    }
}
