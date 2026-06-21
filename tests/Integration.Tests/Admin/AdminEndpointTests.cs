using System.Net;
using System.Net.Http.Json;
using Application.Admin.Dtos;
using Application.Ai;
using Application.Common;
using Application.Grammar.Admin;
using Application.Identity.Dtos;
using Application.Listening.Admin;
using Application.Notifications.Dtos;
using Application.Reading.Admin;
using Application.Vocabulary.Admin;
using Application.Vocabulary.Admin.Images;
using Application.Vocabulary.Ports;
using Application.Writing.Admin;
using Domain.Assessment;
using Domain.Vocabulary;
using FluentAssertions;
using Infrastructure.Images;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Web.Endpoints;
using Xunit;

namespace Integration.Tests.Admin;

/// <summary>
/// End-to-end HTTP tests for the operator admin panel (PROJECT-SPEC operator console).
/// Uses the fresh dev-login flow (POST /api/auth/dev-login) to establish a super-admin
/// session, then exercises every admin endpoint that requires an authenticated caller.
/// Runs on the in-memory adapters (no Postgres / Redis required).
/// </summary>
public class AdminEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor);

    private readonly TestWebApplicationFactory _factory;

    public AdminEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dev_login_returns_user_and_sets_session_cookie()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/dev-login", new { });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ReadJsonAsync<AuthEndpoints.GoogleSignInResponse>(response);
        body.User.Email.Should().Be("javohirsadullayev836@gmail.com");
        body.Token.Should().NotBeNullOrEmpty();
        body.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        response.Headers.Should().ContainKey("Set-Cookie");
    }

    [Fact]
    public async Task Admin_access_returns_super_admin_after_dev_login()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/admin/access");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ReadJsonAsync<AdminAccessDto>(response);
        body.IsAdmin.Should().BeTrue();
        body.CanManageAdmins.Should().BeTrue();
        body.Role.Should().Be(AdminRole.SuperAdmin);
    }

    [Fact]
    public async Task Admin_vocabulary_images_lists_English_and_Uzbek_labels_after_dev_login()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/admin/vocabulary-images");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<IReadOnlyList<VocabularyImageAdminDto>>(response);
        body.Should().NotBeNull();
        if (body.Count > 0)
        {
            body.Should().OnlyContain(item =>
                !string.IsNullOrWhiteSpace(item.Word)
                && !string.IsNullOrWhiteSpace(item.Translation)
                && item.ImageUrl.Contains(item.ImageId.ToString(), StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task Vocabulary_image_admin_endpoint_requires_authentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/vocabulary-images");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_can_list_replace_and_immediately_serve_a_safe_webp_over_http()
    {
        var topic = VocabularyTopic.Curate(
            "a1-family", "Family", "Oila", "people", "present-simple",
            CefrLevel.A1, DateTimeOffset.UtcNow);
        topic.FillContent("My family.", [TopicWord.Create("mother", "ona")]);
        var imageId = WordImageQuery.ImageId(topic.Id, "mother");
        topic.Words[0].SetImage($"local:{imageId}", "old", null);
        var store = new InMemoryTopicImageStore();
        await store.SaveAsync(ToContent(imageId, GeneratedVocabularyImageFactory.Create("old", "eski", 1)), default);

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IVocabularyTopicRepository>();
                services.AddSingleton<IVocabularyTopicRepository>(new SingleTopicRepository(topic));
                services.RemoveAll<ITopicImageStore>();
                services.AddSingleton<ITopicImageStore>(store);
                services.RemoveAll<IImageService>();
                services.AddSingleton<IImageService>(new GeneratedImageService());
            }));
        var client = factory.CreateClient();
        await DevLoginAsync(client);

        var listResponse = await client.GetAsync("/api/admin/vocabulary-images");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var before = await ReadJsonAsync<IReadOnlyList<VocabularyImageAdminDto>>(listResponse);
        before.Should().ContainSingle(item =>
            item.TopicId == topic.Id && item.ImageId == imageId
            && item.Word == "mother" && item.Translation == "ona");

        var replaceResponse = await client.PostAsync(
            $"/api/admin/vocabulary-images/{topic.Id}/{imageId}/replace",
            null);
        replaceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var replaced = await ReadJsonAsync<VocabularyImageAdminDto>(replaceResponse);
        replaced.ImageSource.Should().Be("EnglishAI Generated");
        replaced.Version.Should().BeGreaterThan(0);

        var imageResponse = await client.GetAsync(replaced.ImageUrl);
        imageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        imageResponse.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
        var bytes = await imageResponse.Content.ReadAsByteArrayAsync();
        bytes.AsSpan(0, 4).ToArray().Should().Equal("RIFF"u8.ToArray());
        var persisted = await store.GetByTopicIdAsync(imageId, default);
        persisted.Should().NotBeNull();
        persisted!.SafetyStatus.Should().Be(ImageSafetyStatus.Safe);
        topic.Words[0].ImageSource.Should().Be("EnglishAI Generated");
    }

    [Fact]
    public async Task Developer_api_key_management_is_available_to_the_super_admin()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/developer/keys");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_users_lists_accounts_after_dev_login()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ReadJsonAsync<AdminUsersDto>(response);
        body.ViewerRole.Should().Be(AdminRole.SuperAdmin);
        body.TotalUsers.Should().BeGreaterThanOrEqualTo(1);
        body.Users.Should().Contain(user => user.Email == "javohirsadullayev836@gmail.com");
    }

    [Fact]
    public async Task Admin_server_diagnostics_returns_snapshot_after_dev_login()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/admin/server");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ReadJsonAsync<ServerDiagnosticsDto>(response);
        body.Runtime.MachineName.Should().NotBeNullOrEmpty();
        body.Dependencies.Should().NotBeNull();
        body.Services.Should().NotBeNull();
        body.Logs.Recent.Should().NotBeNull();
    }

    [Fact]
    public async Task Super_admin_can_change_the_daily_variable_cost_budget()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.PutAsJsonAsync(
            "/api/admin/metrics/cost-budget",
            new { dailyBudgetUsd = 50 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<VariableCostSnapshot>(response);
        body.DailyBudgetLimitUsd.Should().Be(50);

        var restore = await client.PutAsJsonAsync(
            "/api/admin/metrics/cost-budget",
            new { dailyBudgetUsd = 25 });
        restore.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000.01)]
    public async Task Daily_variable_cost_budget_rejects_invalid_values(double budget)
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.PutAsJsonAsync(
            "/api/admin/metrics/cost-budget",
            new { dailyBudgetUsd = budget });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_server_stream_returns_initial_history_event()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/server/stream");
        using var response = await client.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellation.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellation.Token);
        using var reader = new StreamReader(stream);
        (await reader.ReadLineAsync(cancellation.Token)).Should().Be("event: history");
        (await reader.ReadLineAsync(cancellation.Token)).Should().StartWith("data: {");
    }

    [Fact]
    public async Task Operations_server_diagnostics_requires_machine_token()
    {
        var client = _factory.CreateClient();

        var missing = await client.GetAsync("/api/operations/server-diagnostics");
        missing.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/operations/server-diagnostics");
        request.Headers.Add("X-Operations-Token", "integration-operations-token");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await ReadJsonAsync<OperationsDiagnosticsDto>(response);
        body.Dependencies.Should().NotBeNull();
        body.Recent.Should().NotBeNull();
        body.Recent.All(entry => entry.Message.Length <= 501).Should().BeTrue();
    }

    [Fact]
    public async Task Admin_broadcasts_returns_empty_history_after_dev_login()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/admin/broadcasts");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ReadJsonAsync<CursorPage<AdminBroadcastDto>>(response);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Admin_vocabulary_returns_typed_catalog_after_dev_login()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/admin/vocabulary");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ReadJsonAsync<IReadOnlyList<VocabularyTopicAdminDto>>(response);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task Admin_grammar_returns_typed_catalog_after_dev_login()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/admin/grammar");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ReadJsonAsync<IReadOnlyList<GrammarLessonAdminDto>>(response);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task Admin_grammar_detail_and_full_update_round_trip()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var createdResponse = await client.PostAsJsonAsync("/api/admin/grammar", new
        {
            title = "Admin detail test",
            category = "Articles",
            level = "A1"
        });
        createdResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var created = await ReadJsonAsync<GrammarLessonAdminDto>(createdResponse);

        var detailResponse = await client.GetAsync($"/api/admin/grammar/{created.Id}");
        detailResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var detail = await ReadJsonAsync<GrammarLessonAdminDetailDto>(detailResponse);
        detail.Status.Should().Be("Pending");

        var updateResponse = await client.PutAsJsonAsync($"/api/admin/grammar/{created.Id}/full", new
        {
            title = "Admin detail updated",
            category = "VerbTense",
            level = "A2",
            status = "Filled",
            vocabularyTopicId = (Guid?)null,
            grammarFocusCode = "past-simple",
            contextIntro = "A complete context introduction.",
            explanation = "A complete rule explanation.",
            curatedTitleUz = "O'zbekcha qoida",
            curatedSummaryUz = "Qisqa qoida mazmuni.",
            curatedFormulas = new[] { "Wh + do + subject + verb?" },
            curatedRules = new[] { new { headingUz = "Tuzilishi", bodyUz = "Wh-so'z gap boshida keladi." } },
            examples = new[] { new { english = "I worked yesterday.", uzbek = "Men kecha ishladim." } },
            commonMistakes = new[] { new { text = "Fe'lga -ed qo'shmaslik." } },
            exercises = new[] { new { type = "Recognition", prompt = "Choose the correct form.", options = new[] { "worked", "work" }, correctOptionIndex = 0, hintCode = (string?)null, explanation = "Past time needs the past form." } },
            applicationTasks = new[] { new { targetSkill = "Speaking", prompt = "Describe yesterday." } }
        });
        updateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var updated = await ReadJsonAsync<GrammarLessonAdminDetailDto>(updateResponse);
        updated.Title.Should().Be("Admin detail updated");
        updated.Exercises.Should().ContainSingle();
        updated.Examples.Should().ContainSingle();
        updated.CuratedRules.Should().ContainSingle();

        var reloaded = await ReadJsonAsync<GrammarLessonAdminDetailDto>(
            await client.GetAsync($"/api/admin/grammar/{created.Id}"));
        reloaded.Explanation.Should().Be("A complete rule explanation.");
        reloaded.Exercises[0].Options.Should().Equal("worked", "work");
        reloaded.CuratedFormulas.Should().ContainSingle("Wh + do + subject + verb?");
    }

    [Fact]
    public async Task Admin_listening_returns_typed_catalog_after_dev_login()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/admin/listening");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ReadJsonAsync<IReadOnlyList<ListeningExerciseAdminDto>>(response);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task Admin_listening_segments_round_trip_through_full_update()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);
        var createdResponse = await client.PostAsJsonAsync("/api/admin/listening", new
        {
            title = "Airport announcement", topic = "Travel", level = "B1"
        });
        createdResponse.EnsureSuccessStatusCode();
        var created = await ReadJsonAsync<ListeningExerciseAdminDto>(createdResponse);

        var updateResponse = await client.PutAsJsonAsync($"/api/admin/listening/{created.Id}/full", new
        {
            title = "Airport announcement", topic = "Travel", level = "B1", status = "Filled",
            vocabularyTopicId = (Guid?)null,
            transcript = "Flight 205 is now boarding at gate seven.",
            segments = new[]
            {
                new { order = 1, startMs = 0, endMs = 2200, speaker = "Announcer", text = "Flight 205 is now boarding." },
                new { order = 2, startMs = 2200, endMs = 4000, speaker = "Announcer", text = "Please go to gate seven." }
            },
            questions = new[]
            {
                new { prompt = "Where should passengers go?", options = new[] { "Gate seven", "Gate five" }, correctOptionIndex = 0, hintCode = (string?)null, explanation = "The announcement says gate seven." }
            }
        });
        updateResponse.EnsureSuccessStatusCode();
        var updated = await ReadJsonAsync<ListeningExerciseAdminDetailDto>(updateResponse);
        updated.Segments.Should().HaveCount(2);
        updated.Segments[0].Speaker.Should().Be("Announcer");

        var reloaded = await ReadJsonAsync<ListeningExerciseAdminDetailDto>(
            await client.GetAsync($"/api/admin/listening/{created.Id}"));
        reloaded.Segments.Select(segment => segment.Order).Should().Equal(1, 2);
        reloaded.Segments[1].Text.Should().Be("Please go to gate seven.");
    }

    [Fact]
    public async Task Admin_reading_returns_typed_catalog_after_dev_login()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/admin/reading");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ReadJsonAsync<IReadOnlyList<ReadingPassageAdminDto>>(response);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task Admin_reading_detail_and_full_update_round_trip()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);
        var createdResponse = await client.PostAsJsonAsync("/api/admin/reading", new
        {
            title = "City gardens", topic = "Nature", level = "B1"
        });
        createdResponse.EnsureSuccessStatusCode();
        var created = await ReadJsonAsync<ReadingPassageAdminDto>(createdResponse);

        var detailResponse = await client.GetAsync($"/api/admin/reading/{created.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await ReadJsonAsync<ReadingPassageAdminDetailDto>(detailResponse);
        detail.Id.Should().Be(created.Id);

        var updateResponse = await client.PutAsJsonAsync($"/api/admin/reading/{created.Id}/full", new
        {
            title = "Updated city gardens", topic = "Nature", category = "environment",
            level = "B1", status = "Filled",
            sections = new[] { "First paragraph.", "Second paragraph." },
            contextGaps = "gap: garden",
            vocabulary = new[] { new { word = "garden", translation = "bog'", exampleSentence = "A city garden." } },
            questions = new[] { new { prompt = "Where is it?", options = new[] { "City", "Village" }, correctOptionIndex = 0, hintCode = "place", explanation = "The passage says city." } }
        });
        updateResponse.EnsureSuccessStatusCode();
        var updated = await ReadJsonAsync<ReadingPassageAdminDetailDto>(updateResponse);
        updated.Title.Should().Be("Updated city gardens");
        updated.Sections.Should().ContainInOrder("First paragraph.", "Second paragraph.");
        updated.Vocabulary.Should().ContainSingle().Which.Translation.Should().Be("bog'");
        updated.Questions.Should().ContainSingle().Which.CorrectOptionIndex.Should().Be(0);
    }

    [Fact]
    public async Task Admin_writing_returns_typed_catalog_after_dev_login()
    {
        var client = _factory.CreateClient();
        await DevLoginAsync(client);

        var response = await client.GetAsync("/api/admin/writing");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await ReadJsonAsync<IReadOnlyList<WritingTaskAdminDto>>(response);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task Admin_access_requires_authentication_without_session()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/access");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_users_requires_authentication_without_session()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/admin/vocabulary")]
    [InlineData("/api/admin/grammar")]
    [InlineData("/api/admin/listening")]
    [InlineData("/api/admin/reading")]
    [InlineData("/api/admin/writing")]
    [InlineData("/api/admin/broadcasts")]
    [InlineData("/api/admin/server")]
    public async Task Admin_catalog_and_operations_endpoints_require_authentication(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Daily_variable_cost_budget_requires_authentication()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/admin/metrics/cost-budget",
            new { dailyBudgetUsd = 50 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_operator_backfill_endpoints_reject_missing_token()
    {
        var client = _factory.CreateClient();

        var backfill = await client.PostAsync("/api/admin/backfill", null);
        backfill.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        var images = await client.PostAsync("/api/admin/backfill-images", null);
        images.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        var audit = await client.PostAsync("/api/admin/images/audit-and-remediate", null);
        audit.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        var status = await client.GetAsync("/api/admin/images/status");
        status.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    private static async Task DevLoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/dev-login", new { });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            "dev-login must succeed so the admin endpoints have a super-admin session");
    }

    private static TopicImageContent ToContent(Guid imageId, DownloadedImage image) => new(
        imageId, 0, image.Data, image.ContentType, image.Source, image.Attribution, image.SourceUrl,
        image.Width, image.Height, DateTimeOffset.UtcNow, SafetyStatus: image.SafetyStatus,
        SafetyModelVersion: image.SafetyModelVersion, SafetyCheckedAt: image.SafetyCheckedAt,
        SafetyReasons: image.SafetyReasons);

    private sealed class GeneratedImageService : IImageService
    {
        public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<ImageResult?>(null);

        public Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<DownloadedImage?>(GeneratedVocabularyImageFactory.Create("mother", "ona", 2));

        public Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
            string query, int count, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DownloadedImage>>(
                [GeneratedVocabularyImageFactory.Create("mother", "ona", 2)]);
    }

    private sealed class SingleTopicRepository(VocabularyTopic topic) : IVocabularyTopicRepository
    {
        public Task<VocabularyTopic?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<VocabularyTopic?>(id == topic.Id ? topic : null);

        public Task<IReadOnlyList<VocabularyTopic>> GetByIdsAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(ids.Contains(topic.Id) ? [topic] : []);

        public Task<IReadOnlyList<VocabularyTopic>> GetByLevelAsync(
            CefrLevel level, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(level == topic.Level ? [topic] : []);

        public Task<IReadOnlyList<VocabularyTopic>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>([topic]);

        public Task SaveAsync(VocabularyTopic value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>();
        body.Should().NotBeNull("the endpoint must return the documented JSON response shape");
        return body!;
    }
}
