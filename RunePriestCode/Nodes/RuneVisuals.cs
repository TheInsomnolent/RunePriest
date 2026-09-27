using Godot;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Nodes;

public enum RuneFamily
{
    Offense,
    Support,
    Resource,
    Cost,
    Modifier,
    Target,
    Flow
}

/// <param name="Symbols">Candidate glyphs; higher values pick later (more complex) characters.</param>
/// <param name="ValueStep">How many points of value advance one character.</param>
public sealed record RuneStyle(RuneFamily Family, string Symbols, int ValueStep = 1);

/// <summary>
/// Colour comes from the rune's family; the script/character comes from its specific effect and value.
/// Every character must be a single UTF-16 unit covered by one of <see cref="RuneFont"/>'s fonts.
/// </summary>
public static class RuneVisuals
{
    private static readonly Dictionary<string, RuneStyle> Styles = new()
    {
        // Kanji ordered by stroke count, so bigger hits look denser.
        ["STRIKE"] = new(RuneFamily.Offense, "刀刃斤矛伐戒刺剣殺斬裂戦撃闘轟", 3),
        ["WEAKENING"] = new(RuneFamily.Offense, "あいうえおかきくけこさしすせそ"),
        ["EXPOSE"] = new(RuneFamily.Offense, "ㄅㄆㄇㄈㄉㄊㄋㄌㄍㄎㄏ"),
        ["VENOM"] = new(RuneFamily.Offense, "㋐㋑㋒㋓㋔㋕㋖㋗㋘㋙"),
        ["BLOCK"] = new(RuneFamily.Support, "αβγδεζηθικλμνξοπρστυφχψωΩ", 2),
        ["MEND"] = new(RuneFamily.Support, "가나다라마바사아자차카타파하"),
        ["BLOOD"] = new(RuneFamily.Cost, "БГДЖЗИЛФЦЧШЩЪЫЭЮЯ"),
        ["KINDLE"] = new(RuneFamily.Resource, "กขคงจฉชซญฎฏฐ"),
        ["SOUL"] = new(RuneFamily.Resource, "アイウエオカキクケコ"),

        ["ADD"] = new(RuneFamily.Modifier, "⊕"),
        ["MULTIPLY"] = new(RuneFamily.Modifier, "⊗"),
        ["ECHO"] = new(RuneFamily.Modifier, "〃"),
        ["SANCTIFY"] = new(RuneFamily.Modifier, "⊘"),

        ["ANCHOR"] = new(RuneFamily.Target, "◎"),
        ["CHAOS"] = new(RuneFamily.Target, "∴"),
        ["NOVA"] = new(RuneFamily.Target, "☆"),
        ["EXECUTION"] = new(RuneFamily.Target, "▽"),
        ["MIRROR"] = new(RuneFamily.Target, "◇"),

        ["LOOP"] = new(RuneFamily.Flow, "〔"),
        ["END_LOOP"] = new(RuneFamily.Flow, "〕"),
        ["SEAL"] = new(RuneFamily.Flow, "〆")
    };

    private static readonly RuneStyle Unknown = new(RuneFamily.Flow, "？");

    public static RuneStyle StyleOf(Rune rune) => Styles.GetValueOrDefault(rune.Key, Unknown);

    public static string SymbolOf(Rune rune)
    {
        var style = StyleOf(rune);
        var index = rune.ShowsValue ? Math.Max(0, rune.Value - 1) / Math.Max(1, style.ValueStep) : 0;
        return RuneFont.Resolve(style.Symbols[Math.Min(index, style.Symbols.Length - 1)].ToString());
    }

    public static Color ColorOf(Rune rune) => StyleOf(rune).Family switch
    {
        RuneFamily.Offense => new Color("ff5a4f"),
        RuneFamily.Support => new Color("5fe38a"),
        RuneFamily.Resource => new Color("ffd447"),
        RuneFamily.Cost => new Color("d81b60"),
        RuneFamily.Modifier => new Color("b07cff"),
        RuneFamily.Target => new Color("ff8ad8"),
        _ => new Color("cfe8ff")
    };

    /// <summary>0..1 visual intensity used for particle count, speed and symbol size.</summary>
    public static float IntensityOf(Rune rune) => rune.ShowsValue ? Mathf.Clamp(rune.Value / 30f, 0.1f, 1f) : 0.25f;
}
