namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// Static analysis of a glyph list: loop bracket matching. No game state; safe to unit test.
/// </summary>
/// <remarks>
/// A <see cref="Mirrored"/> program is a Reflection's reversed tape: read the other way, each End Loop opens a loop and
/// each Loop closes one, so a loop reflects back into a loop.
/// </remarks>
public sealed class RuneProgram
{
    public const int StrayEnd = -1;

    private readonly int[] _match;

    public RuneProgram(IReadOnlyList<Glyph> glyphs, bool mirrored = false)
    {
        Glyphs = glyphs;
        Mirrored = mirrored;
        _match = new int[glyphs.Count];
        var open = new Stack<int>();

        for (var i = 0; i < glyphs.Count; i++)
        {
            if (OpensLoop(glyphs[i], mirrored))
            {
                open.Push(i);
            }
            else if (ClosesLoop(glyphs[i], mirrored))
            {
                if (open.Count == 0)
                {
                    _match[i] = StrayEnd;
                    continue;
                }
                var start = open.Pop();
                _match[start] = i;
                _match[i] = start;
            }
        }

        // Unclosed loops implicitly close at the end of the Incantation.
        while (open.Count > 0)
            _match[open.Pop()] = glyphs.Count;
    }

    public IReadOnlyList<Glyph> Glyphs { get; }

    /// <summary>A Reflection's reversed tape: End Loops open loops and Loops close them.</summary>
    public bool Mirrored { get; }

    /// <summary>Whether <paramref name="glyph"/> opens a loop: a Loop, or an End Loop when <paramref name="mirrored"/>.</summary>
    public static bool OpensLoop(Glyph glyph, bool mirrored) =>
        glyph.Kind == RuneKind.Flow && (mirrored ? glyph.Runes[0] is EndLoopRune : glyph.Runes[0] is LoopRune);

    /// <summary>Whether <paramref name="glyph"/> closes a loop: an End Loop, or a Loop when <paramref name="mirrored"/>.</summary>
    public static bool ClosesLoop(Glyph glyph, bool mirrored) =>
        glyph.Kind == RuneKind.Flow && (mirrored ? glyph.Runes[0] is LoopRune : glyph.Runes[0] is EndLoopRune);

    /// <summary>
    /// For a loop opener: index of its closer, or <c>Glyphs.Count</c> if unclosed.
    /// For a loop closer: index of its opener, or <see cref="StrayEnd"/>.
    /// </summary>
    public int MatchOf(int index) => _match[index];
}
