using System.Xml.Linq;
using Xunit;

namespace Architecture.Tests;

public sealed class RepositoryArchitectureTests
{
    private static readonly string Root = FindRepositoryRoot();

    [Fact]
    public void Project_references_follow_clean_architecture_direction()
    {
        AssertReferences("src/Domain/Domain.csproj");
        AssertReferences("src/Application/Application.csproj", "src/Domain/Domain.csproj");
        AssertReferences("src/Infrastructure/Infrastructure.csproj",
            "src/Application/Application.csproj", "src/Domain/Domain.csproj");
        AssertReferences("apps/api/Api.csproj",
            "src/Application/Application.csproj", "src/Infrastructure/Infrastructure.csproj");
        AssertReferences("apps/worker/Jobs.csproj",
            "src/Application/Application.csproj", "src/Infrastructure/Infrastructure.csproj");
    }

    [Fact]
    public void Canonical_repository_directories_exist()
    {
        foreach (var directory in new[] { "apps", "src", "services", "ops", "tests", "data" })
            Assert.True(Directory.Exists(Path.Combine(Root, directory)), $"Missing canonical directory: {directory}");
    }

    [Fact]
    public void Legacy_compatibility_paths_are_removed()
    {
        foreach (var path in new[]
                 {
                     "frontend", "src/Web", "src/Worker", "youtube-transcript-service",
                     "deploy", "scripts", "tools"
                 })
        {
            var fullPath = Path.Combine(Root, path);
            Assert.False(File.Exists(fullPath) || Directory.Exists(fullPath),
                $"Legacy compatibility path still exists: {path}");
        }
    }

    private static void AssertReferences(string project, params string[] expected)
    {
        var projectPath = Path.Combine(Root, project);
        var document = XDocument.Load(projectPath);
        var actual = document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetRelativePath(Root,
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!,
                    value!.Replace('\\', Path.DirectorySeparatorChar)))))
            .Select(Normalize)
            .OrderBy(value => value)
            .ToArray();

        Assert.Equal(expected.Select(Normalize).OrderBy(value => value), actual);
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EnglishAI.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
