namespace Infrastructure.Speaking;

/// <summary>
/// A curated list of common Uzbek given names fed to the Azure recognizer as a phrase
/// list. The recognizer runs in English (en-US) for the conversation, so without a hint
/// it mis-transcribes Uzbek names the learner says (e.g. their own name when introducing
/// themselves). Boosting these phrases makes recognition of those names reliable without
/// switching the recognition language. Spellings follow the common Latin transliteration.
/// </summary>
internal static class UzbekNamePhrases
{
    public static readonly IReadOnlyList<string> Names = new[]
    {
        // Male
        "Javohir", "Jasur", "Sardor", "Sherzod", "Bekzod", "Bobur", "Aziz", "Azizbek",
        "Akmal", "Alisher", "Anvar", "Asror", "Behruz", "Diyor", "Doniyor", "Eldor",
        "Elyor", "Farrux", "Firdavs", "Husan", "Ilhom", "Islom", "Jahongir", "Kamol",
        "Kamron", "Murod", "Mirjalol", "Nodir", "Nuriddin", "Otabek", "Oybek", "Rustam",
        "Sanjar", "Shoxrux", "Sukhrob", "Sunnatillo", "Temur", "Ulugbek", "Umar",
        "Umid", "Xushnud", "Zafar", "Ziyodullo",
        // Female
        "Sevara", "Madina", "Malika", "Dilnoza", "Dilshoda", "Durdona", "Feruza",
        "Gulnora", "Gulchehra", "Iroda", "Kamola", "Laylo", "Maftuna", "Mahliyo",
        "Marjona", "Munisa", "Nargiza", "Nilufar", "Nodira", "Ozoda", "Rayhona",
        "Robiya", "Sabina", "Sarvinoz", "Shahnoza", "Shaxzoda", "Sitora", "Umida",
        "Yulduz", "Zarina", "Zebo", "Zilola", "Zuhra",
    };
}
