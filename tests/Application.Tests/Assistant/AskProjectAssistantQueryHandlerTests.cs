using Application.Ai;
using Application.Assistant.AskProjectAssistant;
using Application.Assistant.Dtos;
using Application.Assistant.Ports;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Assistant;

public sealed class AskProjectAssistantQueryHandlerTests
{
    [Theory]
    [InlineData("Support uchun kimga bog'lansam bo'ladi?")]
    [InlineData("Qanday aloqa qilsam bo'ladi?")]
    [InlineData("Telegram kanalingiz bormi?")]
    [InlineData("EnglishAI LinkedIn sahifasi kerak")]
    [InlineData("Yordam olish uchun kimga murojaat qilaman?")]
    public async Task Returns_verified_contacts_without_calling_the_provider(string question)
    {
        var assistant = Substitute.For<IProjectAssistant>();

        var result = await new AskProjectAssistantQueryHandler(assistant).Handle(
            new AskProjectAssistantQuery(question, Array.Empty<AssistantTurnDto>()), CancellationToken.None);

        result.Reply.Should()
            .Contain("https://t.me/englishaiuz")
            .And.Contain("https://www.linkedin.com/company/englishai-uz/")
            .And.Contain("https://www.linkedin.com/in/javohir-sadullayev-b8737725a/");
        await assistant.DidNotReceiveWithAnyArgs().AnswerAsync(default!, default!, default!, default);
    }

    [Theory]
    [InlineData("3 yil o'rgandim, nega gapira olmayapman?", "Muammo sizda emas")]
    [InlineData("24/7 AI tutor qanday ishlaydi?", "darajangiz va tanlangan mavzuga mos")]
    [InlineData("6 skill bir mavzuga qanday bog'lanadi?", "Bitta mavzu oltita ko'nikmada")]
    [InlineData("Har darajadagi 50 dars qanday tuzilgan?", "50 ta tartiblangan dars")]
    [InlineData("Books va Video bo'limlarida nima bor?", "interaktiv transkript")]
    [InlineData("EnglishAI bepulmi va qanday boshlayman?", "bepul boshlash mumkin")]
    [InlineData("Telefonda ishlaydimi?", "telefonda ishlaydi")]
    [InlineData("EnglishAI'ni kim yaratgan?", "Javohir Sadullayev")]
    public async Task Returns_verified_product_answers_without_calling_the_provider(string question, string expected)
    {
        var assistant = Substitute.For<IProjectAssistant>();

        var result = await new AskProjectAssistantQueryHandler(assistant).Handle(
            new AskProjectAssistantQuery(question, Array.Empty<AssistantTurnDto>()), CancellationToken.None);

        result.Reply.Should().Contain(expected);
        await assistant.DidNotReceiveWithAnyArgs().AnswerAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Does_not_treat_connected_skills_as_a_contact_question()
    {
        var assistant = Substitute.For<IProjectAssistant>();

        var result = await new AskProjectAssistantQueryHandler(assistant).Handle(
            new AskProjectAssistantQuery("6 skill bir mavzuga qanday bog‘lanadi?", Array.Empty<AssistantTurnDto>()), CancellationToken.None);

        result.Reply.Should().Contain("Bitta mavzu oltita ko'nikmada").And.NotContain("Telegram support");
        await assistant.DidNotReceiveWithAnyArgs().AnswerAsync(default!, default!, default!, default);
    }

    [Theory]
    [InlineData("Present Simple ni tushuntirib ber")]
    [InlineData("Book so'zini o'zbekchaga tarjima qil")]
    public async Task Redirects_learning_questions_without_returning_an_error(string question)
    {
        var assistant = Substitute.For<IProjectAssistant>();

        var result = await new AskProjectAssistantQueryHandler(assistant).Handle(
            new AskProjectAssistantQuery(question, Array.Empty<AssistantTurnDto>()), CancellationToken.None);

        result.Reply.Should().Contain("tizimga kirgandan keyingi AI o'quv yordamchisi");
        await assistant.DidNotReceiveWithAnyArgs().AnswerAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Trims_input_and_limits_history()
    {
        var assistant = Substitute.For<IProjectAssistant>();
        IReadOnlyList<AssistantTurn>? captured = null;
        assistant.AnswerAsync(
                "EnglishAI nima?",
                Arg.Do<IReadOnlyList<AssistantTurn>>(turns => captured = turns),
                "uz",
                Arg.Any<CancellationToken>())
            .Returns("EnglishAI loyiha konsultanti javobi");
        var history = Enumerable.Range(1, 9)
            .Select(index => new AssistantTurnDto(index % 2 == 0 ? "assistant" : "user", $" turn {index} "))
            .ToArray();

        var result = await new AskProjectAssistantQueryHandler(assistant).Handle(
            new AskProjectAssistantQuery(" EnglishAI nima? ", history), CancellationToken.None);

        result.Reply.Should().Be("EnglishAI loyiha konsultanti javobi");
        captured!.Select(turn => turn.Text).Should().Equal("turn 4", "turn 5", "turn 6", "turn 7", "turn 8", "turn 9");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Email orqali ro'yxatdan o'ting va kafolatlangan natija oling.")]
    [InlineData("Qo'shimcha ma'lumot: https://invented.example/pricing")]
    public async Task Replaces_empty_or_unsafe_provider_answers_with_a_verified_fallback(string? providerReply)
    {
        var assistant = Substitute.For<IProjectAssistant>();
        assistant.AnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<AssistantTurn>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(providerReply);

        var result = await new AskProjectAssistantQueryHandler(assistant).Handle(
            new AskProjectAssistantQuery("Platformaning o'ziga xos tomoni nima?", Array.Empty<AssistantTurnDto>()), CancellationToken.None);

        result.Reply.Should().Contain("tasdiqlangan ma'lumot").And.Contain("https://t.me/englishaiuz");
    }

    [Fact]
    public async Task Returns_a_verified_fallback_when_ai_admission_rejects_the_public_request()
    {
        var assistant = Substitute.For<IProjectAssistant>();
        var scope = Substitute.For<IAiFeatureScope>();
        scope.EnterAsync(AiFeature.Assistant, null, Arg.Any<CancellationToken>())
            .Returns<Task<IDisposable>>(_ => throw new AiAdmissionException("rate_limited", "Try later", 60, 429));

        var result = await new AskProjectAssistantQueryHandler(assistant, scope).Handle(
            new AskProjectAssistantQuery("Platformaning o'ziga xos tomoni nima?", Array.Empty<AssistantTurnDto>()), CancellationToken.None);

        result.Reply.Should().Contain("tasdiqlangan ma'lumot");
        await assistant.DidNotReceiveWithAnyArgs().AnswerAsync(default!, default!, default!, default);
    }

    [Theory]
    [InlineData("Why is speaking harder than studying English?", "learning method")]
    [InlineData("How does the AI tutor work?", "guides conversations")]
    [InlineData("How are the six skills connected?", "One topic connects all six skills")]
    [InlineData("How is the learning path organised?", "50 ordered lessons")]
    [InlineData("What can I practise with Books and Video?", "interactive transcripts")]
    [InlineData("How do I contact support?", "For EnglishAI support and official updates")]
    [InlineData("How do I get started for free?", "You can start using EnglishAI for free")]
    [InlineData("What is my level?", "A1 to C2")]
    [InlineData("Does EnglishAI work on my phone?", "EnglishAI works on your phone")]
    [InlineData("Who created EnglishAI?", "was created by")]
    [InlineData("Explain present simple", "This is an English lesson question")]
    [InlineData("Support uchun qanday bog'lanaman?", "For EnglishAI support and official updates")]
    [InlineData("3 yil o'rgandim, nega gapira olmayapman?", "learning method")]
    [InlineData("24/7 AI tutor qanday ishlaydi?", "guides conversations")]
    [InlineData("6 skill bir mavzuga qanday bog'lanadi?", "One topic connects all six skills")]
    [InlineData("Har darajadagi 50 dars qanday tuzilgan?", "50 ordered lessons")]
    [InlineData("Books va Video bo'limlarida nima bor?", "interactive transcripts")]
    public async Task English_locale_returns_verified_English_answers_for_suggestions_and_typed_questions(string question, string expected)
    {
        var assistant = Substitute.For<IProjectAssistant>();
        var result = await new AskProjectAssistantQueryHandler(assistant).Handle(
            new AskProjectAssistantQuery(question, [], "en"), CancellationToken.None);

        result.Reply.Should().Contain(expected).And.NotContain("Bepul boshlash");
        await assistant.DidNotReceiveWithAnyArgs().AnswerAsync(default!, default!, default!, default);
    }

    [Theory]
    [InlineData("en", "A verified EnglishAI answer in English.")]
    [InlineData("uz", "EnglishAI haqida tasdiqlangan o'zbekcha javob.")]
    public async Task Forwards_selected_locale_and_history_to_the_provider(string locale, string reply)
    {
        var assistant = Substitute.For<IProjectAssistant>();
        assistant.AnswerAsync("What makes EnglishAI different?", Arg.Any<IReadOnlyList<AssistantTurn>>(), locale, Arg.Any<CancellationToken>())
            .Returns(reply);
        var result = await new AskProjectAssistantQueryHandler(assistant).Handle(
            new AskProjectAssistantQuery("What makes EnglishAI different?", [new("user", "Old conversation")], locale), CancellationToken.None);

        result.Reply.Should().Be(reply);
        await assistant.Received(1).AnswerAsync("What makes EnglishAI different?",
            Arg.Is<IReadOnlyList<AssistantTurn>>(turns => turns.Count == 1 && turns[0].Text == "Old conversation"),
            locale, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Learn more at https://invented.example/pricing")]
    public async Task English_locale_keeps_provider_failure_and_unsafe_reply_fallbacks_in_English(string? reply)
    {
        var assistant = Substitute.For<IProjectAssistant>();
        assistant.AnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<AssistantTurn>>(), "en", Arg.Any<CancellationToken>())
            .Returns(reply);
        var result = await new AskProjectAssistantQueryHandler(assistant).Handle(
            new AskProjectAssistantQuery("What makes EnglishAI different?", [], "en"), CancellationToken.None);

        result.Reply.Should().StartWith("I can currently share this verified information:")
            .And.Contain("https://t.me/englishaiuz").And.NotContain("tasdiqlangan ma'lumot");
    }

    [Fact]
    public async Task English_locale_keeps_admission_failure_in_English()
    {
        var assistant = Substitute.For<IProjectAssistant>();
        var scope = Substitute.For<IAiFeatureScope>();
        scope.EnterAsync(AiFeature.Assistant, null, Arg.Any<CancellationToken>())
            .Returns<Task<IDisposable>>(_ => throw new AiAdmissionException("rate_limited", "Try later", 60, 429));
        var result = await new AskProjectAssistantQueryHandler(assistant, scope).Handle(
            new AskProjectAssistantQuery("What makes EnglishAI different?", [], "en"), CancellationToken.None);

        result.Reply.Should().StartWith("I can currently share this verified information:");
        await assistant.DidNotReceiveWithAnyArgs().AnswerAsync(default!, default!, default!, default);
    }

    [Theory]
    [InlineData("How does the AI tutor work?", "darajangiz va tanlangan mavzuga mos")]
    [InlineData("How do I contact support?", "rasmiy yangiliklari")]
    public async Task Page_locale_not_question_language_controls_verified_answers(string question, string expected)
    {
        var result = await new AskProjectAssistantQueryHandler(Substitute.For<IProjectAssistant>()).Handle(
            new AskProjectAssistantQuery(question, [], "uz"), CancellationToken.None);
        result.Reply.Should().Contain(expected);
    }
}
