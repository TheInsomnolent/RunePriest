namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// Text form of glyphs, used to store runes Imbued into cards: glyphs separated by <c>;</c>, runes by <c>,</c>,
/// each rune as <c>KEY:VALUE</c> (e.g. <c>STRIKE:10,BLOOD:3;AMPLIFY:2</c>). Anchors and sources are not stored.
/// Unknown runes are dropped, so a corrupt glyph decodes as malformed and simply fizzles when spoken.
/// </summary>
public static class GlyphCodec
{
    public static string Encode(IEnumerable<Glyph> glyphs) =>
        string.Join(";", glyphs.Select(g => string.Join(",", g.Runes.Select(r => $"{r.Key}:{r.Value}"))));

    public static IReadOnlyList<Glyph> Decode(string? code)
    {
        if (string.IsNullOrEmpty(code)) return [];
        return code.Split(';')
            .Select(g => Glyph.Of(g.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Parse).OfType<Rune>().ToArray()))
            .ToList();
    }

    private static Rune? Parse(string text)
    {
        var parts = text.Split(':');
        var value = parts.Length > 1 && int.TryParse(parts[1], out var v) ? v : 0;
        return Create(parts[0], value);
    }

    /// <summary>Builds a rune from its <see cref="Rune.Key"/> and value, or null if the key is unknown.</summary>
    public static Rune? Create(string key, int value) => key switch
    {
        "STRIKE" => new StrikeRune(value),
        "DEFEND" => new DefendRune(value),
        "MEND" => new MendRune(value),
        "CLEANSE" => new CleanseRune(),
        "BLOOD" => new BloodRune(value),
        "KINDLE" => new KindleRune(value),
        "SWIFT" => new SwiftRune(value),
        "HEX" => new HexRune(value),
        "DIMINISH" => new DiminishRune(value),
        "AMPLIFY" => new AmplifyRune(value),
        "TWIN" => new TwinRune(value),
        "ECHO" => new EchoRune(value),
        "SANCTIFY" => new SanctifyRune(),
        "VOID" => new VoidRune(),
        "GROWTH" => new GrowthRune(value),
        "REFLECTION" => new ReflectionRune(),
        "CLONE" => new CloneRune(),
        "FRIENDSHIP" => new FriendshipRune(value),
        "LOOP" => new LoopRune(),
        "END_LOOP" => new EndLoopRune(),
        "SEAL" => new SealRune(),
        _ when Enum.TryParse<TargetMode>(key, true, out var mode) && !int.TryParse(key, out _) => new TargetRune(mode),
        _ => null
    };
}
