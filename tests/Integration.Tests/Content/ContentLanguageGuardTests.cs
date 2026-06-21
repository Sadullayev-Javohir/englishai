using FluentAssertions;
using Infrastructure.Common;
using Xunit;

namespace Integration.Tests.Content;

public class ContentLanguageGuardTests
{
    [Theory]
    [InlineData("O'zbekcha va English text, 100% clean!")]
    [InlineData("g'oya, o'rganish, sh, ch, ng")]
    [InlineData("Line one.\nLine two?")]
    public void Accepts_English_and_Uzbek_ASCII_text(string text) =>
        ContentLanguageGuard.IsClean(text).Should().BeTrue();

    [Theory]
    [InlineData("Кирилл")]
    [InlineData("العربية")]
    [InlineData("中文")]
    [InlineData("한국어")]
    [InlineData("français")]
    [InlineData("smart quote ’")]
    [InlineData("emoji 🙂")]
    public void Rejects_every_non_ASCII_character(string text) =>
        ContentLanguageGuard.IsClean(text).Should().BeFalse();

    [Theory]
    // The Uzbek okina (U+02BB) and tutuq belgisi (U+02BC) - typographically correct Uzbek, and by far
    // the most common non-ASCII characters a model emits when writing o'z / g'oya / so'z.
    [InlineData("o\u02BBz va g\u02BBoya", "o'z va g'oya")]
    [InlineData("bu so\u02BCz", "bu so'z")]
    // Curly quotes, dashes, ellipsis, non-breaking space and zero-width marks.
    [InlineData("\u201Chabit\u201D so\u2019zi", "\"habit\" so'zi")]
    [InlineData("odat \u2014 takrorlash", "odat - takrorlash")]
    [InlineData("shuning uchun\u2026", "shuning uchun...")]
    [InlineData("ikki\u00A0so'z", "ikki so'z")]
    [InlineData("zero\u200Bwidth", "zerowidth")]
    public void Normalizes_typographic_look_alikes_to_ASCII(string raw, string expected)
    {
        var normalized = ContentLanguageGuard.NormalizeToAscii(raw);

        normalized.Should().Be(expected);
        ContentLanguageGuard.IsClean(normalized).Should()
            .BeTrue("normalizing punctuation must let an otherwise-clean reply through the guard");
    }

    [Theory]
    // Script contamination is NOT typography: normalizing must leave it in place so the guard still
    // rejects the reply (docs/development-guide.md rule 11).
    [InlineData("Bu video 习惯 haqida.")]
    [InlineData("Бу видео.")]
    [InlineData("Bu vidéo odatlar haqida.")]
    [InlineData("emoji 🙂")]
    public void Leaves_non_target_scripts_for_the_guard_to_reject(string raw) =>
        ContentLanguageGuard.IsClean(ContentLanguageGuard.NormalizeToAscii(raw)).Should().BeFalse();

    [Fact]
    public void Returns_plain_ASCII_text_unchanged()
    {
        const string text = "Bu video odatlar haqida. 'Habit' so'zi odat degani.";

        ContentLanguageGuard.NormalizeToAscii(text).Should().BeSameAs(text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Passes_empty_input_through(string? text) =>
        ContentLanguageGuard.NormalizeToAscii(text).Should().Be(text);
}
