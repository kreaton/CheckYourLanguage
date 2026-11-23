namespace CheckYourLanguage.Game;

/// <summary>How the phrases come across.</summary>
public enum Mode
{
    /// <summary>Each side is spoken as it comes up; its text is shown when asked for (or always, with
    /// <see cref="PhraseGame.AlwaysText"/>).</summary>
    Listen,

    /// <summary>Each side is shown as text; tapping its speaker plays it.</summary>
    Read,

    /// <summary>Text only, no sound at all.</summary>
    Silent,
}

public enum Stage
{
    /// <summary>The phrase has been heard in the first language; the second is still to come.</summary>
    Heard,

    /// <summary>The phrase has been heard in the second language as well; waiting for the next one.</summary>
    Translated,
}

/// <summary>
/// A random phrase in one language, then the same phrase in another. Every phrase comes once, in random
/// order, before any repeats. The text on each side stays hidden until it is asked for, unless
/// <see cref="AlwaysText"/> is on.
/// </summary>
public sealed class PhraseGame(PhraseBook book)
{
    private readonly Random _random = new();
    private readonly Queue<int> _bag = new();

    public PhraseBook Book { get; } = book;

    /// <summary>The language the phrase is heard in first.</summary>
    public Language From { get; private set; } = book.Languages[0];

    /// <summary>The language it is heard in next.</summary>
    public Language To { get; private set; } = book.Languages[Math.Min(1, book.Languages.Count - 1)];

    /// <summary>The current phrase, or null before the first one.</summary>
    public int? Current { get; private set; }

    public Stage Stage { get; private set; }

    public bool FromTextShown { get; private set; }
    public bool ToTextShown { get; private set; }

    /// <summary>Listen: shows the text on both sides straight away, from the next phrase on.</summary>
    public bool AlwaysText { get; set; }

    public Mode Mode { get; private set; } = Mode.Listen;

    /// <summary>Whether the text is shown as soon as a side comes up: always, except in Listen without
    /// <see cref="AlwaysText"/>.</summary>
    public bool TextFirst => AlwaysText || Mode != Mode.Listen;

    /// <summary>Whether a side is spoken as soon as it comes up.</summary>
    public bool AutoPlay => Mode == Mode.Listen;

    /// <summary>Whether there is any sound (the speakers are shown).</summary>
    public bool HasSound => Mode != Mode.Silent;

    /// <summary>Changes the mode. Any text the new mode always shows is shown at once.</summary>
    public void SetMode(Mode mode)
    {
        Mode = mode;
        if (TextFirst) ShowText();
    }

    /// <summary>How many phrases have come up in this pass through the list.</summary>
    public int Seen => Book.Count - _bag.Count;

    /// <summary>The phrase after the current one, so its audio can be fetched ahead.</summary>
    public int? Upcoming => _bag.TryPeek(out var next) ? next : null;

    public string FromText => Current is { } p ? Book.Text(p, From) : "";
    public string ToText => Current is { } p ? Book.Text(p, To) : "";
    public string FromAudio => Current is { } p ? Book.Audio(p, From) : "";
    public string ToAudio => Current is { } p ? Book.Audio(p, To) : "";

    /// <summary>Sets the first language. Picking the one already second swaps the two.</summary>
    public void SetFrom(Language language)
    {
        if (language == From) return;
        if (language == To) To = From;
        From = language;
        ResetText();
    }

    /// <summary>Sets the second language. Picking the one already first swaps the two.</summary>
    public void SetTo(Language language)
    {
        if (language == To) return;
        if (language == From) From = To;
        To = language;
        ResetText();
    }

    public void Swap()
    {
        (From, To) = (To, From);
        ResetText();
    }

    private void ResetText()
    {
        FromTextShown = TextFirst;
        ToTextShown = TextFirst;
    }

    public void NextPhrase()
    {
        if (Book.Count == 0) return;
        if (_bag.Count == 0)
        {
            var shuffled = Enumerable.Range(0, Book.Count).ToArray();
            _random.Shuffle(shuffled);
            if (shuffled.Length > 1 && shuffled[0] == Current)
                (shuffled[0], shuffled[^1]) = (shuffled[^1], shuffled[0]);
            foreach (var phrase in shuffled) _bag.Enqueue(phrase);
        }

        Current = _bag.Dequeue();
        Stage = Stage.Heard;
        ResetText();
    }

    /// <summary>Moves on to the second language.</summary>
    public void Translate()
    {
        if (Current is not null) Stage = Stage.Translated;
    }

    public void ShowFromText() => FromTextShown = Current is not null;

    public void ShowToText() => ToTextShown = Current is not null && Stage == Stage.Translated;

    /// <summary>Shows the text of every side heard so far.</summary>
    public void ShowText()
    {
        ShowFromText();
        ShowToText();
    }
}
