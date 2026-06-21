using System.Net.Http.Headers;
using System.Text.Json;
using Application.Common;

namespace Infrastructure.Images;

public sealed class HttpImageSafetyClassifier(HttpClient http) : IImageSafetyClassifier
{
    public async Task<ImageSafetyDecision> ClassifyAsync(
        byte[] data,
        string contentType,
        CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        using var content = new ByteArrayContent(data);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(content, "file", "image");

        using var response = await http.PostAsync("/v1/classify", form, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<Response>(
                         stream,
                         new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                         cancellationToken)
            ?? throw new InvalidDataException("Image moderation returned an empty response.");
        if (string.IsNullOrWhiteSpace(result.ModelVersion))
            throw new InvalidDataException("Image moderation omitted modelVersion.");

        var reasons = result.Reasons?
            .Where(reason => !string.IsNullOrWhiteSpace(reason.Label))
            .Select(reason => $"{reason.Label}:{reason.Score:0.000}")
            .ToArray() ?? [];
        return new ImageSafetyDecision(result.Safe, result.ModelVersion, reasons);
    }

    private sealed record Response(bool Safe, string ModelVersion, Reason[]? Reasons);
    private sealed record Reason(string Label, double Score);
}
