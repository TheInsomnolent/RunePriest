namespace RunePriest.RunePriestCode.Runes;

/// <summary>The ordered glyphs of one player's Incantation. UI observes <see cref="Changed"/>.</summary>
public sealed class RuneBuffer
{
    private readonly List<Glyph> _glyphs = [];

    public IReadOnlyList<Glyph> Glyphs => _glyphs;

    public bool IsSpeaking { get; internal set; }

    public event Action? Changed;

    /// <summary>Raised each time a glyph is evaluated (repeatedly inside loops).</summary>
    public event Action<Glyph>? GlyphActivated;

    public event Action<Glyph>? GlyphFizzled;

    /// <summary>Raised after Speak finishes and <see cref="IsSpeaking"/> is false again.</summary>
    public event Action? SpeakEnded;

    /// <summary>Raised when a glyph is swapped in place (e.g. merged), before <see cref="Changed"/>.</summary>
    public event Action<Glyph, Glyph>? GlyphReplaced;

    internal void NotifyActivated(Glyph glyph) => GlyphActivated?.Invoke(glyph);

    internal void NotifyFizzled(Glyph glyph) => GlyphFizzled?.Invoke(glyph);

    internal void NotifySpeakEnded() => SpeakEnded?.Invoke();

    public void Inscribe(IEnumerable<Glyph> glyphs)
    {
        _glyphs.AddRange(glyphs);
        Changed?.Invoke();
    }

    /// <summary>Inserts glyphs at <paramref name="index"/> (0 = the start of the Incantation).</summary>
    public void Insert(int index, IEnumerable<Glyph> glyphs)
    {
        _glyphs.InsertRange(Math.Clamp(index, 0, _glyphs.Count), glyphs);
        Changed?.Invoke();
    }

    public IReadOnlyList<Glyph> TakeAll()
    {
        var taken = _glyphs.ToList();
        _glyphs.Clear();
        Changed?.Invoke();
        return taken;
    }

    /// <summary>Puts glyphs back at the front (e.g. everything after a Seal).</summary>
    public void Retain(IReadOnlyList<Glyph> glyphs)
    {
        if (glyphs.Count == 0) return;
        _glyphs.InsertRange(0, glyphs);
        Changed?.Invoke();
    }

    public Glyph? RemoveAt(int index)
    {
        if (index < 0 || index >= _glyphs.Count) return null;
        var glyph = _glyphs[index];
        _glyphs.RemoveAt(index);
        Changed?.Invoke();
        return glyph;
    }

    public void Replace(int index, Glyph glyph)
    {
        var old = _glyphs[index];
        _glyphs[index] = glyph;
        GlyphReplaced?.Invoke(old, glyph);
        Changed?.Invoke();
    }
}
