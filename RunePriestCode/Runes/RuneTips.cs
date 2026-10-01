using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace RunePriest.RunePriestCode.Runes;

public static class RuneTips
{
    public const string Table = "static_hover_tips";
    public const string Prefix = "RUNEPRIEST-";

    public static IHoverTip Inscribe => Tip("INSCRIBE");
    public static IHoverTip Speak => Tip("SPEAK");
    public static IHoverTip Imbue => Tip("IMBUE");
    public static IHoverTip Overflow => Tip("OVERFLOW");
    public static IHoverTip Fizzle => Tip("FIZZLE");

    /// <summary>Lists the runes bound to an Imbued card.</summary>
    public static IHoverTip Imbued(IEnumerable<Glyph> glyphs)
    {
        var description = new LocString(Table, Prefix + "IMBUED.description");
        description.Add("Runes", string.Join("\n", glyphs.Select(g => g.Label)));
        return new HoverTip(new LocString(Table, Prefix + "IMBUED.title"), description);
    }

    /// <summary>
    /// Card text for Inscribing <paramref name="glyphs"/>: one Inscribe line per glyph, runes of a compound glyph
    /// separated by commas (e.g. "Inscribe Strike 10, Blood 3."). Used to show what an Imbued card now does.
    /// </summary>
    public static string InscribeText(IEnumerable<Glyph> glyphs) =>
        string.Join("\n", glyphs.Where(g => g.Runes.Count > 0).Select(g =>
        {
            var line = new LocString(Table, Prefix + "IMBUED.inscribe");
            line.Add("Runes", string.Join(", ", g.Runes.Select(r =>
                $"[blue]{r.TitleLoc.GetFormattedText()}[/blue]" + (r.ShowsValue ? $" {r.ValueLabel}" : ""))));
            return line.GetFormattedText();
        }));

    public static LocString IncantationScriptTitle => new(Table, Prefix + "INCANTATION_SCRIPT.title");

    public static LocString ForecastTitle => new(Table, Prefix + "FORECAST.title");

    public static string Capacity(int count, int capacity)
    {
        var loc = new LocString(Table, Prefix + "INCANTATION_SCRIPT.capacity");
        loc.Add("Count", count);
        loc.Add("Capacity", capacity);
        return loc.GetFormattedText();
    }

    public static string FormatForecast(RunePreview preview)
    {
        var lines = preview.Lines
            .Select(l => l.Hits > 1
                ? $"{l.Rune.TitleLoc.GetFormattedText()} ×{l.Hits}: {l.Total}"
                : $"{l.Rune.TitleLoc.GetFormattedText()}: {l.Total}")
            .ToList();

        if (preview.Fizzles > 0)
        {
            var fizzles = new LocString(Table, Prefix + "FORECAST.fizzles");
            fizzles.Add("Count", preview.Fizzles);
            lines.Add(fizzles.GetFormattedText());
        }
        if (preview.Overloaded)
            lines.Add(new LocString(Table, Prefix + "FORECAST.overload").GetFormattedText());

        lines.Add(new LocString(Table, Prefix + "FORECAST.note").GetFormattedText());
        return string.Join("\n", lines);
    }

    private static HoverTip Tip(string key) =>
        new(new LocString(Table, Prefix + key + ".title"), new LocString(Table, Prefix + key + ".description"));
}
