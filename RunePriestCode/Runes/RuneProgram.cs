namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// Static analysis of a glyph list: loop bracket matching. No game state; safe to unit test.
/// </summary>
public sealed class RuneProgram
{
    public const int StrayEnd = -1;

    private readonly int[] _match;

    public RuneProgram(IReadOnlyList<Glyph> glyphs)
    {
        Glyphs = glyphs;
        _match = new int[glyphs.Count];
        var open = new Stack<int>();

        for (var i = 0; i < glyphs.Count; i++)
        {
            if (glyphs[i].Kind != RuneKind.Flow) continue;

            switch (glyphs[i].Runes[0])
            {
                case LoopRune:
                    open.Push(i);
                    break;
                case EndLoopRune when open.Count == 0:
                    _match[i] = StrayEnd;
                    break;
                case EndLoopRune:
                    var start = open.Pop();
                    _match[start] = i;
                    _match[i] = start;
                    break;
            }
        }

        // Unclosed loops implicitly close at the end of the Incantation.
        while (open.Count > 0)
            _match[open.Pop()] = glyphs.Count;
    }

    public IReadOnlyList<Glyph> Glyphs { get; }

    /// <summary>
    /// For a Loop: index of its End Loop, or <c>Glyphs.Count</c> if unclosed.
    /// For an End Loop: index of its Loop, or <see cref="StrayEnd"/>.
    /// </summary>
    public int MatchOf(int index) => _match[index];
}
