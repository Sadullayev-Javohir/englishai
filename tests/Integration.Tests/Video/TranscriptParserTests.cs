using System.Text.Json;
using FluentAssertions;
using Infrastructure.Video;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the json3 transcript parser: it must lift YouTube's real per-word timing
/// (<c>tStartMs</c> + a seg's <c>tOffsetMs</c>), drop the newline-only "rolling" events, and
/// re-group the word stream into readable sentences - by a speech pause, sentence punctuation,
/// or a maximum word count - so the player shows whole sentences with a karaoke word highlight.
/// </summary>
public class TranscriptParserTests
{
    // Mirrors a real auto-caption shape: a window-define event, "\n"-only rolling events (noise),
    // content events whose segs carry per-word tOffsetMs, no punctuation, and a long pause.
    private const string AutoCaptionJson3 = """
        {"events":[
          {"tStartMs":0,"dDurationMs":900000,"id":1},
          {"tStartMs":1000,"dDurationMs":900,"aAppend":1,"segs":[{"utf8":"\n"}]},
          {"tStartMs":1000,"dDurationMs":900,"segs":[
            {"utf8":"this"},{"utf8":" program","tOffsetMs":200},{"utf8":" is","tOffsetMs":600}]},
          {"tStartMs":1900,"dDurationMs":900,"aAppend":1,"segs":[{"utf8":"\n"}]},
          {"tStartMs":1900,"dDurationMs":900,"segs":[
            {"utf8":"brought"},{"utf8":" to","tOffsetMs":200},{"utf8":" you","tOffsetMs":500}]},
          {"tStartMs":9000,"dDurationMs":900,"segs":[
            {"utf8":"thank"},{"utf8":" you","tOffsetMs":400}]}
        ]}
        """;

    private static IReadOnlyList<Application.Video.Models.TranscriptLine> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return TranscriptParser.ParseTimedTextJson3(doc.RootElement);
    }

    [Fact]
    public void Groups_words_into_sentences_split_on_a_speech_pause()
    {
        var lines = Parse(AutoCaptionJson3);

        // The 6.6s gap before "thank" splits the stream into two sentences; the "\n" noise events
        // contribute no words.
        lines.Should().HaveCount(2);
        lines[0].EnglishText.Should().Be("this program is brought to you");
        lines[1].EnglishText.Should().Be("thank you");
    }

    [Fact]
    public void Lifts_real_per_word_timing_from_tStartMs_plus_tOffsetMs()
    {
        var words = Parse(AutoCaptionJson3)[0].Words!;

        words.Should().HaveCount(6);
        words[0].Text.Should().Be("this");
        words[0].StartSeconds.Should().BeApproximately(1.0, 0.001);   // tStartMs 1000
        words[1].Text.Should().Be("program");
        words[1].StartSeconds.Should().BeApproximately(1.2, 0.001);   // 1000 + tOffsetMs 200
        words[2].StartSeconds.Should().BeApproximately(1.6, 0.001);   // 1000 + 600

        // A word lasts until the next word starts...
        words[0].EndSeconds.Should().BeApproximately(1.2, 0.001);
        // ...but the last word before the long pause is capped so the highlight does not linger.
        words[5].Text.Should().Be("you");
        words[5].EndSeconds.Should().BeApproximately(words[5].StartSeconds + 1.2, 0.001);
    }

    [Fact]
    public void Splits_on_sentence_punctuation_when_present()
    {
        // Manual captions can carry punctuation; a word ending in '.'/'?'/'!' closes the sentence.
        var lines = Parse("""
            {"events":[
              {"tStartMs":0,"dDurationMs":900,"segs":[
                {"utf8":"Hello"},{"utf8":" there.","tOffsetMs":300},
                {"utf8":" Welcome","tOffsetMs":700},{"utf8":" in.","tOffsetMs":1100}]}
            ]}
            """);

        lines.Should().HaveCount(2);
        lines[0].EnglishText.Should().Be("Hello there.");
        lines[1].EnglishText.Should().Be("Welcome in.");
    }

    [Fact]
    public void Caps_sentence_length_for_a_long_unpunctuated_run()
    {
        // 30 words, no punctuation, no pause - must break into readable chunks, not one giant line.
        var segs = string.Join(",", Enumerable.Range(0, 30)
            .Select(i => $"{{\"utf8\":\"{(i == 0 ? "" : " ")}word{i}\",\"tOffsetMs\":{i * 250}}}"));
        var lines = Parse($"{{\"events\":[{{\"tStartMs\":0,\"dDurationMs\":9000,\"segs\":[{segs}]}}]}}");

        lines.Should().HaveCountGreaterThan(1);
        lines.Should().OnlyContain(l => l.Words!.Count <= 14);
        lines.Sum(l => l.Words!.Count).Should().Be(30);
    }

    [Fact]
    public void Strips_speaker_change_markers_from_words_and_text()
    {
        // YouTube captions mark a speaker change with '>>'. It is notation, not a spoken word, so it
        // must never appear in the sentence text or the karaoke word stream.
        var lines = Parse("""
            {"events":[
              {"tStartMs":0,"dDurationMs":900,"segs":[
                {"utf8":">>"},{"utf8":" Could","tOffsetMs":100},{"utf8":" you.","tOffsetMs":400},
                {"utf8":" >> Sure.","tOffsetMs":900}]}
            ]}
            """);

        lines.SelectMany(l => l.Words!).Should().NotContain(w => w.Text.Contains('>'));
        lines.Should().OnlyContain(l => !l.EnglishText.Contains('>'));
        lines[0].EnglishText.Should().Be("Could you.");
        lines[1].EnglishText.Should().Be("Sure.");
    }

    [Fact]
    public void Splits_a_manual_tracks_whole_line_seg_into_individual_words()
    {
        // Manual caption tracks put a whole line (words separated by spaces and a YouTube &nbsp;\n)
        // in ONE seg, unlike auto tracks' one-word-per-seg shape. The parser must split it into real
        // words - never keep the whole 60+ char line as a single "word" (which overflows the Text
        // column and silently fails the transcript save, leaving the lesson stuck "pending").
        var lines = Parse("""
            {"events":[
              {"tStartMs":1000,"dDurationMs":2000,"segs":[
                {"utf8":"Welcome to Real Easy English from \nBBC Learning English."}]},
              {"tStartMs":3000,"dDurationMs":2000,"segs":[
                {"utf8":"I'm Beth and this is Neil."}]}
            ]}
            """);

        var allWords = lines.SelectMany(l => l.Words!).ToList();
        allWords.Should().Contain(w => w.Text == "Welcome");
        allWords.Should().Contain(w => w.Text == "English.");
        allWords.Should().Contain(w => w.Text == "Beth");
        // No word is a run-on line, and every word fits the 60-char storage column.
        allWords.Should().OnlyContain(w => w.Text.Length <= 60 && !w.Text.Contains(' '));
        // Words stay in spoken order across the split.
        var ordered = allWords.Select(w => w.StartSeconds).ToList();
        ordered.Should().BeInAscendingOrder();
        lines[0].EnglishText.Should().StartWith("Welcome to Real Easy English");
    }

    [Fact]
    public void Truncates_a_pathological_overlong_token_so_the_save_cannot_overflow()
    {
        var url = new string('x', 200);
        var lines = Parse($$"""
            {"events":[
              {"tStartMs":0,"dDurationMs":1000,"segs":[{"utf8":"visit {{url}} now."}]}
            ]}
            """);

        lines.SelectMany(l => l.Words!).Should().OnlyContain(w => w.Text.Length <= 60);
    }

    [Fact]
    public void Returns_empty_when_there_are_no_events()
    {
        Parse("""{"foo":"bar"}""").Should().BeEmpty();
    }

    private static IReadOnlyList<Application.Video.Models.TranscriptLine> ParseSupadata(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return TranscriptParser.ParseSupadataTranscript(doc.RootElement);
    }

    [Fact]
    public void Parses_supadata_content_into_sentences_with_per_word_timing()
    {
        // Supadata returns line-level chunks ({text, offset, duration} in ms, no per-word times). The
        // parser must split each chunk into words spread across its duration and re-group into the same
        // sentence/karaoke shape a native fetch yields, so the player highlights words identically.
        var lines = ParseSupadata("""
            {"content":[
              {"text":"Hello there.","offset":1000,"duration":900,"lang":"en"},
              {"text":"Welcome to the show.","offset":2000,"duration":1200,"lang":"en"}
            ],"lang":"en","availableLangs":["en"]}
            """);

        lines.Should().HaveCount(2);
        lines[0].EnglishText.Should().Be("Hello there.");
        lines[1].EnglishText.Should().Be("Welcome to the show.");

        var first = lines[0].Words!;
        first.Should().HaveCount(2);
        first[0].Text.Should().Be("Hello");
        first[0].StartSeconds.Should().BeApproximately(1.0, 0.001); // offset 1000ms

        // Every word keeps real ascending timing and fits the storage column.
        var allWords = lines.SelectMany(l => l.Words!).ToList();
        allWords.Select(w => w.StartSeconds).Should().BeInAscendingOrder();
        allWords.Should().OnlyContain(w => w.Text.Length <= 60 && !w.Text.Contains(' '));
    }

    [Fact]
    public void Returns_empty_when_supadata_content_is_missing()
    {
        ParseSupadata("""{"error":"transcript-unavailable"}""").Should().BeEmpty();
        ParseSupadata("""{"content":[]}""").Should().BeEmpty();
    }
}
