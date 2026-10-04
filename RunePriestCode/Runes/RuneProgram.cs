namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// Static analysis of a glyph list: loop bracket matching. No game state; safe to unit test.
/// </summary>
/// <remarks>
/// The Speak can run either way (a Reflection turns it around). Read <paramref name="reversed"/> (right to left), each
/// End Loop opens a loop and each Loop closes one, so a loop reflects back into a loop; brackets are matched for both
/// directions.
/// </remarks>
public sealed class RuneProgram
{
    public const int StrayEnd = int.MinValue;

    private readonly int[] _forward;
    private readonly int[] _reversed;

    public RuneProgram(IReadOnlyList<Glyph> glyphs)
    {
        Glyphs = glyphs;
        _forward = Match(glyphs, reversed: false);
        _reversed = Match(glyphs, reversed: true);
    }

    public IReadOnlyList<Glyph> Glyphs { get; }

    /// <summary>Whether <paramref name="glyph"/> opens a loop: a Loop, or an End Loop when Spoken <paramref name="reversed"/>.</summary>
    public static bool OpensLoop(Glyph glyph, bool reversed) =>
        glyph.Kind == RuneKind.Flow && (reversed ? glyph.Runes[0] is EndLoopRune : glyph.Runes[0] is LoopRune);

    /// <summary>Whether <paramref name="glyph"/> closes a loop: an End Loop, or a Loop when Spoken <paramref name="reversed"/>.</summary>
    public static bool ClosesLoop(Glyph glyph, bool reversed) =>
        glyph.Kind == RuneKind.Flow && (reversed ? glyph.Runes[0] is LoopRune : glyph.Runes[0] is EndLoopRune);

    /// <summary>
    /// Read in the given direction: for a loop opener, the index of its closer (unclosed: one step past the last glyph,
    /// <c>Glyphs.Count</c> or <c>-1</c> reversed); for a loop closer, the index of its opener, or <see cref="StrayEnd"/>.
    /// </summary>
    public int MatchOf(int index, bool reversed = false) => (reversed ? _reversed : _forward)[index];

    private static int[] Match(IReadOnlyList<Glyph> glyphs, bool reversed)
    {
        var match = new int[glyphs.Count];
        var open = new Stack<int>();
        var step = reversed ? -1 : 1;

        for (var i = reversed ? glyphs.Count - 1 : 0; i >= 0 && i < glyphs.Count; i += step)
        {
            if (OpensLoop(glyphs[i], reversed))
            {
                open.Push(i);
            }
            else if (ClosesLoop(glyphs[i], reversed))
            {
                if (open.Count == 0)
                {
                    match[i] = StrayEnd;
                    continue;
                }
                var start = open.Pop();
                match[start] = i;
                match[i] = start;
            }
        }

        // Unclosed loops implicitly close at the end of the Incantation.
        while (open.Count > 0)
            match[open.Pop()] = reversed ? -1 : glyphs.Count;
        return match;
    }
}
