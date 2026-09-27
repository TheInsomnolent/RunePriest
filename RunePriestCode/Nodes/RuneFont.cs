using Godot;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// Composite font built from the game's own locale fonts (always present in the base pck), so exotic scripts render
/// regardless of the player's language without shipping extra font files.
/// </summary>
public static class RuneFont
{
    private static readonly string[] Paths =
    [
        "res://themes/fonts/jpn/noto_sans_cjkjp_regular_shared.tres",
        "res://themes/fonts/kor/gyeonggi_cheonnyeon_batang_bold_shared.tres",
        "res://themes/fonts/tha/cs_chat_thai_ui_shared.tres",
        "res://themes/fonts/rus/fira_sans_extra_condensed_regular_shared.tres"
    ];

    private const string Fallback = "◆";

    private static List<Font>? _fonts;
    private static Font? _composite;

    private static List<Font> Fonts => _fonts ??= Paths
        .Where(p => ResourceLoader.Exists(p))
        .Select(p => ResourceLoader.Load<Font>(p, null, ResourceLoader.CacheMode.Reuse))
        .Where(f => f != null)
        .ToList();

    /// <summary>Null if none of the game fonts could be loaded (the label then uses the theme default).</summary>
    public static Font? Font
    {
        get
        {
            if (_composite != null || Fonts.Count == 0) return _composite;
            _composite = new FontVariation
            {
                BaseFont = Fonts[0],
                Fallbacks = new Godot.Collections.Array<Font>(Fonts.Skip(1))
            };
            return _composite;
        }
    }

    /// <summary>Returns the symbol if some font can draw it, otherwise a safe fallback.</summary>
    public static string Resolve(string symbol)
    {
        if (Fonts.Count == 0) return symbol;
        var codepoint = char.ConvertToUtf32(symbol, 0);
        if (Fonts.Any(f => f.HasChar(codepoint))) return symbol;

        MainFile.Logger.Warn($"[Rune] No font glyph for U+{codepoint:X4}; using fallback");
        return Fallback;
    }
}
