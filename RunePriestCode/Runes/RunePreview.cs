namespace RunePriest.RunePriestCode.Runes;

/// <summary>Accumulated totals from a dry-run of the Incantation, after Strength, Weak, Vulnerable, Frail, etc.</summary>
public sealed class RunePreview
{
    private readonly Dictionary<string, (PayloadRune Rune, int Base, int Total, int Hits)> _totals = [];
    private readonly Dictionary<(Glyph, int), int> _shown = [];

    public int Fizzles { get; set; }
    public bool Overloaded { get; set; }

    /// <summary>Inscribed glyphs that (or whose carry-overs) stay in the Incantation after this Speak.</summary>
    public HashSet<Glyph> Persisting { get; } = [];
    public bool IsEmpty => _totals.Count == 0 && Fizzles == 0 && !Overloaded;

    /// <param name="value">Before game effects.</param>
    /// <param name="modified">After game effects (<see cref="PayloadRune.Modified"/>).</param>
    public void Record(PayloadRune rune, int value, int modified)
    {
        var (sample, total, modifiedTotal, hits) = _totals.GetValueOrDefault(rune.Key, (rune, 0, 0, 0));
        _totals[rune.Key] = (sample, total + value, modifiedTotal + modified, hits + 1);
    }

    /// <summary>An inscribed rune's own value after game effects, from the first time it resolves (overhead UI).</summary>
    public void Show(Glyph glyph, int runeIndex, int modified) => _shown.TryAdd((glyph, runeIndex), modified);

    public int? ShownValue(Glyph glyph, int runeIndex) => _shown.TryGetValue((glyph, runeIndex), out var v) ? v : null;

    /// <summary>One line per payload type; <c>Base</c> is the total before game effects.</summary>
    public IEnumerable<(PayloadRune Rune, int Base, int Total, int Hits)> Lines => _totals.Values;
}
