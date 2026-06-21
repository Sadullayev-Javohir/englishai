using System.Text.Json;
using Infrastructure.Common;

namespace Infrastructure.Curriculum;

public sealed record CurriculumValidation(bool Approved, IReadOnlyList<string> Issues)
{
    public string ToJson() => JsonSerializer.Serialize(this);
}

public static class CurriculumPayloadValidator
{
    public static CurriculumValidation Validate(string module, string json)
    {
        var issues = new List<string>();
        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException) { return new(false, new[] { "invalid_json" }); }
        using (doc)
        {
            ValidateElement(doc.RootElement, issues);
            switch (module)
            {
                case "vocabulary": ValidateVocabulary(doc.RootElement, issues); break;
                case "grammar": ValidateGrammar(doc.RootElement, issues); break;
                case "reading": RequireString(doc.RootElement, "body", issues); ValidateArray(doc.RootElement, "glossary", 1, issues); ValidateQuestions(doc.RootElement, "questions", 6, issues); break;
                case "listening": RequireString(doc.RootElement, "transcript", issues); ValidateQuestions(doc.RootElement, "questions", 6, issues); break;
                case "speaking": RequireString(doc.RootElement, "objective", issues); ValidateArray(doc.RootElement, "priorityWords", 10, issues); ValidateArray(doc.RootElement, "questions", 6, issues); RequireObject(doc.RootElement, "rubric", issues); break;
                case "books": RequireString(doc.RootElement, "body", issues); ValidateQuestions(doc.RootElement, "questions", 10, issues); break;
                case "writing": RequireString(doc.RootElement, "prompt", issues); ValidateArray(doc.RootElement, "guidance", 3, issues); break;
            }
        }
        return new(issues.Count == 0, issues.Distinct().ToArray());
    }

    private static void ValidateElement(JsonElement element, List<string> issues)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var normalized = ContentLanguageGuard.NormalizeToAscii(element.GetString());
            if (!ContentLanguageGuard.IsClean(normalized)) issues.Add("forbidden_script");
            return;
        }
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject()) ValidateElement(property.Value, issues);
        if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) ValidateElement(child, issues);
    }

    private static void ValidateVocabulary(JsonElement root, List<string> issues)
    {
        RequireString(root, "passage", issues);
        if (!root.TryGetProperty("words", out var words) || words.ValueKind != JsonValueKind.Array || words.GetArrayLength() != 20)
        { issues.Add("vocabulary_requires_20_items"); return; }
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in words.EnumerateArray())
        {
            foreach (var field in new[] { "w", "uz", "ex", "category" }) RequireString(word, field, issues);
            if (word.TryGetProperty("w", out var value) && value.ValueKind == JsonValueKind.String && !unique.Add(value.GetString()!.Trim()))
                issues.Add("duplicate_vocabulary_item");
        }
    }

    private static void ValidateGrammar(JsonElement root, List<string> issues)
    {
        RequireAnyString(root, new[] { "intro", "contextIntro", "introduction" }, "intro", issues);
        RequireAnyString(root, new[] { "rule", "explanation", "grammarRule" }, "rule", issues);
        ValidateArray(root, "examples", 4, issues); ValidateArray(root, "tasks", 2, issues);
        ValidateQuestions(root, root.TryGetProperty("exercises", out _) ? "exercises" : "questions", 10, issues);
    }

    private static void ValidateQuestions(JsonElement root, string name, int count, List<string> issues)
    {
        if (!root.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() != count)
        { issues.Add($"{name}_requires_{count}"); return; }
        foreach (var q in array.EnumerateArray())
        {
            if (!HasAnyString(q, "q", "prompt", "question")) issues.Add("missing_q");
            if (!q.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() is < 3 or > 4)
                issues.Add("question_options_3_or_4");
            if (!q.TryGetProperty("answer", out var answer) || answer.ValueKind != JsonValueKind.Number ||
                options.ValueKind != JsonValueKind.Array || answer.GetInt32() < 0 || answer.GetInt32() >= options.GetArrayLength())
                issues.Add("invalid_answer_index");
        }
    }

    private static void ValidateArray(JsonElement root, string name, int minimum, List<string> issues)
    {
        if (!root.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() < minimum)
            issues.Add($"{name}_minimum_{minimum}");
    }

    private static void RequireString(JsonElement root, string name, List<string> issues)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            issues.Add($"missing_{name}");
    }

    private static void RequireObject(JsonElement root, string name, List<string> issues)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Object)
            issues.Add($"missing_{name}");
    }

    private static void RequireAnyString(JsonElement root, IReadOnlyList<string> names, string issueName, List<string> issues)
    {
        if (!names.Any(name => HasAnyString(root, name))) issues.Add($"missing_{issueName}");
    }

    private static bool HasAnyString(JsonElement root, params string[] names) => names.Any(name =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()));
}
