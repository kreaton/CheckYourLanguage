namespace CheckYourLanguage.Game;

public sealed class Language
{
    public required string Code { get; init; }

    /// <summary>The English name, used in prompts ("Hear it in Danish").</summary>
    public required string Name { get; init; }

    /// <summary>The language's own name, shown as its button's tooltip.</summary>
    public required string Native { get; init; }

    /// <summary>The country whose flag is on the button (wwwroot/flags/{flag}.svg); the code when not set.</summary>
    public string? Flag { get; init; }

    public string FlagFile => $"flags/{Flag ?? Code}.svg";

    /// <summary>Every language the app knows, in button order. Only those in phrases.json are shown.</summary>
    public static readonly IReadOnlyList<Language> Known =
    [
        new() { Code = "da", Name = "Danish", Native = "Dansk", Flag = "dk" },
        new() { Code = "en", Name = "English", Native = "English", Flag = "gb" },
        new() { Code = "de", Name = "German", Native = "Deutsch" },
        // Swiss Standard German (a Swiss voice, standard words), in the folder audio/ch.
        new() { Code = "ch", Name = "Swiss German", Native = "Deutsch (CH)" },
        new() { Code = "fr", Name = "French", Native = "Français" },
        new() { Code = "it", Name = "Italian", Native = "Italiano" },
        new() { Code = "es", Name = "Spanish", Native = "Español" },
        new() { Code = "nl", Name = "Dutch", Native = "Nederlands" },
    ];
}
