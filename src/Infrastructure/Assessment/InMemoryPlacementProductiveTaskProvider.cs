using Application.Assessment.Ports;
using Domain.Assessment;
using Infrastructure.Common;

namespace Infrastructure.Assessment;

/// <summary>
/// In-memory bank of curated placement productive tasks (one Writing and one Speaking
/// prompt per CEFR level). Prompts are English content (data, not generated). For a
/// requested difficulty the nearest available level is chosen, deterministically, so the
/// same task can be re-resolved when the learner submits.
/// </summary>
public sealed class InMemoryPlacementProductiveTaskProvider : IPlacementProductiveTaskProvider
{
    private static readonly IReadOnlyDictionary<CefrLevel, PlacementWritingTask> WritingTasks =
        new[]
        {
            W("pw-a1", CefrLevel.A1, "Write a few sentences about yourself: your name, your age, and where you live.", 25, 50),
            W("pw-a2", CefrLevel.A2, "Write a short message to a friend describing what you usually do at the weekend.", 35, 70),
            W("pw-b1", CefrLevel.B1, "Write a short text about a place you would like to visit and explain why you want to go there.", 50, 100),
            W("pw-b2", CefrLevel.B2, "Some people prefer to study online, others in a classroom. Write a short text giving your opinion and reasons.", 70, 140),
            W("pw-c1", CefrLevel.C1, "\"Technology has made people less social.\" Write a short argumentative text discussing whether you agree, with examples.", 90, 180),
            W("pw-c2", CefrLevel.C2, "Discuss the view that economic growth and protecting the environment cannot go together. Support your argument with reasons and examples.", 110, 220),
        }.ToDictionary(t => t.Difficulty);

    private static readonly IReadOnlyDictionary<CefrLevel, PlacementSpeakingTask> SpeakingTasks =
        new[]
        {
            S("ps-a1", CefrLevel.A1, "Talk about your family for about 30 seconds. Who are they and what do they do?", 15),
            S("ps-a2", CefrLevel.A2, "Describe your daily routine. What do you do in the morning, afternoon, and evening?", 20),
            S("ps-b1", CefrLevel.B1, "Talk about a hobby or activity you enjoy and explain why you like it.", 30),
            S("ps-b2", CefrLevel.B2, "Describe an important decision you have made and explain how it changed your life.", 40),
            S("ps-c1", CefrLevel.C1, "Some say social media does more harm than good. Give your opinion and support it with reasons.", 50),
            S("ps-c2", CefrLevel.C2, "Discuss whether governments should fund space exploration when many problems remain on Earth.", 55),
        }.ToDictionary(t => t.Difficulty);

    public PlacementWritingTask GetWritingTask(CefrLevel difficulty) =>
        Nearest(WritingTasks, difficulty);

    public PlacementSpeakingTask GetSpeakingTask(CefrLevel difficulty) =>
        Nearest(SpeakingTasks, difficulty);

    private static PlacementWritingTask W(
        string key, CefrLevel level, string prompt, int minWords, int maxWords) =>
        PlacementWritingTask.Create(DeterministicGuid.Create(key), level, prompt, minWords, maxWords);

    private static PlacementSpeakingTask S(
        string key, CefrLevel level, string prompt, int minWords) =>
        PlacementSpeakingTask.Create(DeterministicGuid.Create(key), level, prompt, minWords);

    private static T Nearest<T>(IReadOnlyDictionary<CefrLevel, T> bank, CefrLevel difficulty)
    {
        if (bank.TryGetValue(difficulty, out var exact))
            return exact;

        // Fall back to the closest available level (ties resolve to the easier one).
        return bank
            .OrderBy(kv => Math.Abs((int)kv.Key - (int)difficulty))
            .ThenBy(kv => (int)kv.Key)
            .First()
            .Value;
    }
}
