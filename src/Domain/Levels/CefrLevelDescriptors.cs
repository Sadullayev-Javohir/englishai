using Domain.Assessment;
using Domain.Learning;

namespace Domain.Levels;

/// <summary>
/// The curated CEFR "can-do" descriptor catalog (PROJECT-SPEC M.1). Each level exposes one
/// statement for each of the four communicative skills (Listening, Reading, Speaking, Writing),
/// phrased from the Council of Europe CEFR Global Scale. These are shown on the Level Map so the
/// learner sees concretely what a level means, and they anchor the Level Exit Test design (M.6).
/// English text is canonical here; the Uzbek shown to learners comes from vetted templates keyed
/// by <see cref="CefrLevelDescriptor.StatementCode"/> (rule 11).
/// </summary>
public static class CefrLevelDescriptors
{
    /// <summary>The skills described, in display order (the four communicative skills, M.1).</summary>
    public static readonly IReadOnlyList<SkillType> DescribedSkills = new[]
    {
        SkillType.Listening,
        SkillType.Reading,
        SkillType.Speaking,
        SkillType.Writing,
    };

    private static readonly IReadOnlyDictionary<(CefrLevel, SkillType), string> Statements =
        new Dictionary<(CefrLevel, SkillType), string>
        {
            [(CefrLevel.A1, SkillType.Listening)] = "I can understand familiar everyday words and very simple phrases when people speak slowly and clearly.",
            [(CefrLevel.A1, SkillType.Reading)] = "I can understand familiar names, words and very simple sentences, for example on signs and notices.",
            [(CefrLevel.A1, SkillType.Speaking)] = "I can introduce myself and others, and ask and answer simple questions about personal details.",
            [(CefrLevel.A1, SkillType.Writing)] = "I can write a short, simple message and fill in forms with personal details.",

            [(CefrLevel.A2, SkillType.Listening)] = "I can understand phrases and the most common vocabulary about areas of immediate personal relevance.",
            [(CefrLevel.A2, SkillType.Reading)] = "I can read short, simple texts and find specific information in everyday material like menus and timetables.",
            [(CefrLevel.A2, SkillType.Speaking)] = "I can communicate in simple, routine tasks and describe my background and immediate surroundings.",
            [(CefrLevel.A2, SkillType.Writing)] = "I can write short, simple notes and a simple personal letter, for example thanking someone.",

            [(CefrLevel.B1, SkillType.Listening)] = "I can understand the main points of clear standard speech on familiar matters at work, school and leisure.",
            [(CefrLevel.B1, SkillType.Reading)] = "I can understand texts that consist mainly of everyday or job-related language.",
            [(CefrLevel.B1, SkillType.Speaking)] = "I can deal with most situations while travelling and connect phrases to describe experiences and events.",
            [(CefrLevel.B1, SkillType.Writing)] = "I can write simple connected text on familiar topics and personal letters describing experiences.",

            [(CefrLevel.B2, SkillType.Listening)] = "I can understand extended speech and most TV news and current-affairs programmes.",
            [(CefrLevel.B2, SkillType.Reading)] = "I can read articles and reports on contemporary problems in which writers take particular stances.",
            [(CefrLevel.B2, SkillType.Speaking)] = "I can interact with fluency and spontaneity and explain a viewpoint on a topical issue.",
            [(CefrLevel.B2, SkillType.Writing)] = "I can write clear, detailed text on a wide range of subjects and essays passing on information.",

            [(CefrLevel.C1, SkillType.Listening)] = "I can understand extended speech even when it is not clearly structured and relationships are implied.",
            [(CefrLevel.C1, SkillType.Reading)] = "I can understand long and complex factual and literary texts and appreciate differences in style.",
            [(CefrLevel.C1, SkillType.Speaking)] = "I can express ideas fluently and spontaneously and use language flexibly for social and professional purposes.",
            [(CefrLevel.C1, SkillType.Writing)] = "I can express myself in clear, well-structured text and write about complex subjects in detail.",

            [(CefrLevel.C2, SkillType.Listening)] = "I have no difficulty understanding any kind of spoken language, including fast native speech.",
            [(CefrLevel.C2, SkillType.Reading)] = "I can read with ease virtually all forms of the written language, including abstract and complex texts.",
            [(CefrLevel.C2, SkillType.Speaking)] = "I can take part effortlessly in any conversation and express myself precisely in complex situations.",
            [(CefrLevel.C2, SkillType.Writing)] = "I can write clear, smoothly flowing text in an appropriate style and complex letters, reports or articles.",
        };

    /// <summary>The four can-do descriptors for a level, in <see cref="DescribedSkills"/> order.</summary>
    public static IReadOnlyList<CefrLevelDescriptor> For(CefrLevel level) =>
        DescribedSkills
            .Select(skill => new CefrLevelDescriptor(level, skill, Statements[(level, skill)]))
            .ToList();
}
