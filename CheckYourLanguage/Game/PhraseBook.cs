namespace CheckYourLanguage.Game;

/// <summary>wwwroot/phrases.json, as written by tools/import-phrases.ps1.</summary>
public sealed record PhraseData(string[] Languages, PhraseRow[] Phrases);

/// <summary>One phrase: its text and audio file name (without extension) in each language, in the order of
/// <see cref="PhraseData.Languages"/>.</summary>
public sealed record PhraseRow(string[] Text, string[] File);

/// <summary>The phrases, with the same meaning in every language.</summary>
public sealed class PhraseBook
{
    private readonly Dictionary<string, int> _column;
    private readonly PhraseRow[] _rows;

    public PhraseBook(PhraseData data)
    {
        _column = data.Languages.Select((code, i) => (code, i)).ToDictionary(x => x.code, x => x.i);
        _rows = data.Phrases.Where(r => r.Text.Length == data.Languages.Length && r.File.Length == data.Languages.Length)
            .ToArray();
        Languages = Language.Known.Where(l => _column.ContainsKey(l.Code)).ToList();
    }

    /// <summary>The languages that have phrases, in button order.</summary>
    public IReadOnlyList<Language> Languages { get; }

    public int Count => _rows.Length;

    public string Text(int phrase, Language language) => _rows[phrase].Text[_column[language.Code]];

    public string Audio(int phrase, Language language) =>
        $"audio/{language.Code}/{Uri.EscapeDataString(_rows[phrase].File[_column[language.Code]])}.mp3";

    public Language Get(string? code, Language fallback) => Languages.FirstOrDefault(l => l.Code == code) ?? fallback;
}
