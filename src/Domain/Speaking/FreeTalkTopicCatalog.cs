using System.Security.Cryptography;
using System.Text;
using Domain.Assessment;
using Domain.Common;

namespace Domain.Speaking;

/// <summary>
/// A single curated free-talk conversation subject the learner can pick under "Erkin suhbat"
/// (free conversation). The <see cref="Code"/> is a stable snake_case English code sent to the
/// tutor as the conversation topic (the tutor renders it as human-readable text and anchors the
/// chat on it - see the conversation tutor). <see cref="EnglishTitle"/> is a reference/debug label;
/// the learner-facing Uzbek label lives in the frontend content store keyed by <see cref="Code"/>
/// (docs/development-guide.md rule 11). Pure curated data, so it lives in the domain and is server-authoritative:
/// the client only ever sends the code, never free text.
/// </summary>
public sealed class FreeTalkTopic
{
    public FreeTalkTopic(string code, string englishTitle, CefrLevel level)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Free-talk topic code must not be empty.");
        if (string.IsNullOrWhiteSpace(englishTitle))
            throw new DomainException("Free-talk topic English title must not be empty.");

        Code = code;
        EnglishTitle = englishTitle;
        Level = level;
        ImageId = CreateImageId(code);
    }

    /// <summary>Stable snake_case code sent as the conversation topic and used to key Uzbek labels.</summary>
    public string Code { get; }

    /// <summary>English title (reference/debug); the UI shows the Uzbek label from the content store.</summary>
    public string EnglishTitle { get; }

    /// <summary>The CEFR level this topic is pitched at.</summary>
    public CefrLevel Level { get; }

    /// <summary>
    /// A stable, deterministic id (derived from <see cref="Code"/>) that keys this topic's
    /// illustration in the shared topic-image store, so free-talk topics reuse the whole
    /// licensed-image pipeline (download once, store in DB, serve from /api/images/topics/{id} -
    /// rule 12) without needing their own table. Deterministic so a re-seeded catalog keeps its
    /// images. Namespaced so it never collides with a roleplay scenario's image id.
    /// </summary>
    public Guid ImageId { get; }

    // Mirrors Infrastructure.Common.DeterministicGuid (MD5 of the key) - duplicated here because the
    // domain references nothing. Not security-relevant; only a reproducible id.
    private static Guid CreateImageId(string code) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes($"free-talk-topic:{code}")));
}

/// <summary>
/// The fixed catalog of free-talk conversation topics: exactly <see cref="TopicsPerLevel"/> topics
/// for each CEFR level (A1→C2), <see cref="TotalTopics"/> in total. Curated content (docs/development-guide.md:
/// not AI-invented), kept as a static domain table like <see cref="RoleplayScenarioCatalog"/> so the
/// topics query and any prompt builder resolve exactly the same definitions. Difficulty steps up by
/// level: concrete everyday subjects at A1/A2, opinions and experiences at B1/B2, and abstract,
/// argumentative themes at C1/C2.
/// </summary>
public static class FreeTalkTopicCatalog
{
    /// <summary>How many topics every CEFR level offers.</summary>
    public const int TopicsPerLevel = 20;

    /// <summary>Total topics across all six CEFR levels.</summary>
    public const int TotalTopics = TopicsPerLevel * 6;

    private static readonly IReadOnlyList<FreeTalkTopic> Topics = BuildCatalog();

    /// <summary>All topics, ordered by level (A1→C2) then by their curated order within the level.</summary>
    public static IReadOnlyList<FreeTalkTopic> All => Topics;

    /// <summary>The 20 topics curated for a single CEFR level, in their curated order.</summary>
    public static IReadOnlyList<FreeTalkTopic> ForLevel(CefrLevel level) =>
        Topics.Where(t => t.Level == level).ToList();

    private static IReadOnlyList<FreeTalkTopic> BuildCatalog()
    {
        var topics = new List<FreeTalkTopic>(TotalTopics);

        void AddLevel(CefrLevel level, params (string Code, string Title)[] entries)
        {
            foreach (var (code, title) in entries)
                topics.Add(new FreeTalkTopic(code, title, level));
        }

        // A1 - concrete, everyday, high-frequency subjects.
        AddLevel(
            CefrLevel.A1,
            ("my_family", "My family"),
            ("my_daily_routine", "My daily routine"),
            ("favorite_food", "My favorite food"),
            ("my_house", "My house"),
            ("colors_and_numbers", "Colors and numbers"),
            ("my_pet", "My pet"),
            ("days_of_the_week", "Days of the week"),
            ("the_weather_today", "The weather today"),
            ("my_friends", "My friends"),
            ("clothes_i_wear", "Clothes I wear"),
            ("my_school", "My school"),
            ("fruits_and_vegetables", "Fruits and vegetables"),
            ("my_hobbies", "My hobbies"),
            ("body_parts", "Parts of the body"),
            ("my_hometown", "My hometown"),
            ("shopping_for_food", "Shopping for food"),
            ("my_morning", "My morning"),
            ("telling_the_time", "Telling the time"),
            ("my_favorite_animal", "My favorite animal"),
            ("greetings_and_names", "Greetings and names"));

        // A2 - everyday life, slightly extended, simple past/future.
        AddLevel(
            CefrLevel.A2,
            ("weekend_plans", "Weekend plans"),
            ("my_last_holiday", "My last holiday"),
            ("going_to_the_doctor", "Going to the doctor"),
            ("my_favorite_movie", "My favorite movie"),
            ("describing_a_friend", "Describing a friend"),
            ("shopping_for_clothes", "Shopping for clothes"),
            ("my_neighborhood", "My neighborhood"),
            ("getting_around_town", "Getting around town"),
            ("my_favorite_season", "My favorite season"),
            ("cooking_a_meal", "Cooking a meal"),
            ("birthday_celebrations", "Birthday celebrations"),
            ("my_free_time", "My free time"),
            ("making_plans_with_friends", "Making plans with friends"),
            ("describing_your_town", "Describing your town"),
            ("healthy_habits", "Healthy habits"),
            ("my_favorite_sport", "My favorite sport"),
            ("a_typical_workday", "A typical workday"),
            ("using_the_phone", "Using the phone"),
            ("asking_for_directions", "Asking for directions"),
            ("my_dream_vacation", "My dream vacation"));

        // B1 - opinions, experiences, connected discourse.
        AddLevel(
            CefrLevel.B1,
            ("social_media_habits", "Social media habits"),
            ("learning_a_language", "Learning a language"),
            ("my_future_goals", "My future goals"),
            ("work_life_balance", "Work-life balance"),
            ("favorite_books", "Favorite books"),
            ("travel_experiences", "Travel experiences"),
            ("technology_in_daily_life", "Technology in daily life"),
            ("music_and_concerts", "Music and concerts"),
            ("environmental_habits", "Environmental habits"),
            ("staying_healthy", "Staying healthy"),
            ("memorable_events", "Memorable events"),
            ("city_vs_countryside", "City vs countryside"),
            ("shopping_online", "Shopping online"),
            ("celebrations_and_traditions", "Celebrations and traditions"),
            ("a_person_you_admire", "A person you admire"),
            ("part_time_jobs", "Part-time jobs"),
            ("food_and_culture", "Food and culture"),
            ("weekend_getaways", "Weekend getaways"),
            ("my_daily_challenges", "My daily challenges"),
            ("hobbies_and_interests", "Hobbies and interests"));

        // B2 - abstract themes, argument and comparison.
        AddLevel(
            CefrLevel.B2,
            ("remote_work", "Remote work"),
            ("the_role_of_technology", "The role of technology"),
            ("climate_change", "Climate change"),
            ("education_systems", "Education systems"),
            ("social_media_influence", "Social media's influence"),
            ("cultural_differences", "Cultural differences"),
            ("healthy_lifestyle_choices", "Healthy lifestyle choices"),
            ("career_ambitions", "Career ambitions"),
            ("traveling_the_world", "Traveling the world"),
            ("money_and_saving", "Money and saving"),
            ("news_and_media", "News and media"),
            ("art_and_creativity", "Art and creativity"),
            ("urban_life", "Urban life"),
            ("volunteering", "Volunteering"),
            ("work_stress", "Work stress"),
            ("the_future_of_jobs", "The future of jobs"),
            ("relationships_and_friendship", "Relationships and friendship"),
            ("personal_development", "Personal development"),
            ("living_abroad", "Living abroad"),
            ("entertainment_and_hobbies", "Entertainment and hobbies"));

        // C1 - nuanced, societal and evaluative topics.
        AddLevel(
            CefrLevel.C1,
            ("globalization", "Globalization"),
            ("artificial_intelligence", "Artificial intelligence"),
            ("ethics_in_business", "Ethics in business"),
            ("the_economy", "The economy"),
            ("mental_health_awareness", "Mental health awareness"),
            ("the_media_and_truth", "The media and truth"),
            ("sustainable_living", "Sustainable living"),
            ("leadership_qualities", "Leadership qualities"),
            ("innovation_and_society", "Innovation and society"),
            ("cultural_identity", "Cultural identity"),
            ("the_value_of_education", "The value of education"),
            ("privacy_in_the_digital_age", "Privacy in the digital age"),
            ("work_and_automation", "Work and automation"),
            ("social_inequality", "Social inequality"),
            ("the_arts_in_society", "The arts in society"),
            ("science_and_progress", "Science and progress"),
            ("consumerism", "Consumerism"),
            ("the_meaning_of_success", "The meaning of success"),
            ("urbanization", "Urbanization"),
            ("lifelong_learning", "Lifelong learning"));

        // C2 - sophisticated, philosophical and specialized themes.
        AddLevel(
            CefrLevel.C2,
            ("the_philosophy_of_happiness", "The philosophy of happiness"),
            ("free_will_and_determinism", "Free will and determinism"),
            ("the_ethics_of_ai", "The ethics of AI"),
            ("geopolitics", "Geopolitics"),
            ("the_nature_of_creativity", "The nature of creativity"),
            ("economic_globalization", "Economic globalization"),
            ("the_future_of_humanity", "The future of humanity"),
            ("language_and_thought", "Language and thought"),
            ("moral_dilemmas", "Moral dilemmas"),
            ("the_role_of_government", "The role of government"),
            ("scientific_ethics", "Scientific ethics"),
            ("art_and_meaning", "Art and meaning"),
            ("justice_and_law", "Justice and law"),
            ("the_information_age", "The information age"),
            ("cultural_relativism", "Cultural relativism"),
            ("the_psychology_of_decisions", "The psychology of decisions"),
            ("power_and_responsibility", "Power and responsibility"),
            ("the_limits_of_knowledge", "The limits of knowledge"),
            ("technology_and_ethics", "Technology and ethics"),
            ("the_pursuit_of_wisdom", "The pursuit of wisdom"));

        return topics;
    }
}
