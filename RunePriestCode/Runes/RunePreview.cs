namespace RunePriest.RunePriestCode.Runes;

/// <summary>Accumulated totals from a dry-run of the Incantation (base values: before Strength, Vulnerable, etc.).</summary>
public sealed class RunePreview
{
    private readonly Dictionary<string, (PayloadRune Sample, int Total, int Hits)> _totals = [];

    public int Fizzles { get; set; }
    public bool Overloaded { get; set; }
    public bool IsEmpty => _totals.Count == 0 && Fizzles == 0 && !Overloaded;

    public void Record(PayloadRune rune, int value)
    {
        var (sample, total, hits) = _totals.GetValueOrDefault(rune.Key, (rune, 0, 0));
        _totals[rune.Key] = (sample, total + value, hits + 1);
    }

    /// <summary>One line per payload type, e.g. "Strike 42 (3 hits)".</summary>
    public IEnumerable<(PayloadRune Rune, int Total, int Hits)> Lines =>
        _totals.Values.Select(v => (v.Sample, v.Total, v.Hits));
}
