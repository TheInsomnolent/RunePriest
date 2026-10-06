namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// Text form of glyphs, used to store runes Imbued into cards: glyphs separated by <c>;</c>, runes by <c>,</c>,
/// each rune as <c>KEY:VALUE</c> (e.g. <c>STRIKE:10,BLOOD:3;AMPLIFY:2</c>); a Persistent glyph starts with <c>!</c>
/// and a <see cref="Rune.Radiant"/> rune with <c>*</c> (e.g. <c>!*HEX:1</c>). A heavy Strike adds its bonus per rune
/// (<c>STRIKE:12:3</c>). Anchors and sources are not stored.
/// Unknown runes are dropped, so a corrupt glyph decodes as malformed and simply fizzles when spoken.
/// </summary>
public static class GlyphCodec
{
    private const char PersistMark = '!';
    private const char RadiantMark = '*';

    public static string Encode(IEnumerable<Glyph> glyphs) =>
        string.Join(";", glyphs.Select(g =>
            (g.Persistent ? PersistMark.ToString() : "") + string.Join(",", g.Runes.Select(r => $"{(r.Radiant ? RadiantMark.ToString() : "")}{r.Key}:{r.Value}{(r is StrikeRune { IsHeavy: true } s ? $":{s.BonusPerRune}" : "")}"))));

    public static IReadOnlyList<Glyph> Decode(string? code)
    {
        if (string.IsNullOrEmpty(code)) return [];
        return code.Split(';').Select(DecodeGlyph).ToList();
    }

    private static Glyph DecodeGlyph(string text)
    {
        var persistent = text.StartsWith(PersistMark);
        var glyph = Glyph.Of(text.TrimStart(PersistMark).Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(Parse).OfType<Rune>().ToArray());
        return persistent ? glyph.Persist() : glyph;
    }

    private static Rune? Parse(string text)
    {
        var radiant = text.StartsWith(RadiantMark);
        var parts = text.TrimStart(RadiantMark).Split(':');
        var value = parts.Length > 1 && int.TryParse(parts[1], out var v) ? v : 0;
        var rune = Create(parts[0], value);
        if (rune is StrikeRune && parts.Length > 2 && int.TryParse(parts[2], out var bonus)) rune = new StrikeRune(value, bonus);
        return rune?.RadiantIf(radiant);
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
        "OVERGROWTH" => new OvergrowthRune(value),
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
