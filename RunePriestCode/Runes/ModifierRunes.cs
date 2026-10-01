using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace RunePriest.RunePriestCode.Runes;

/// <summary>Applies to the next glyph (target glyphs are transparent). Before a Loop, applies to the whole body.</summary>
public abstract class ModifierRune(int value) : Rune(value)
{
    public override RuneKind Kind => RuneKind.Modifier;

    /// <summary>The raw effect on a value. Use <see cref="ApplyTo"/>, which respects <see cref="Rune.Amplifiable"/>.</summary>
    protected virtual int Apply(PayloadRune rune, int value) => value;

    /// <summary>Modified value of <paramref name="rune"/>; runes that aren't amplifiable are never changed.</summary>
    public int ApplyTo(PayloadRune rune, int value) => rune.Amplifiable ? Apply(rune, value) : value;

    public virtual int ExtraExecutions => 0;

    /// <summary>If true, the next glyph (or every glyph of a loop body) fizzles instead of resolving.</summary>
    public virtual bool Voids => false;
}

public sealed class AmplifyRune(int value) : ModifierRune(value)
{
    public override string Key => "AMPLIFY";
    public override Rune WithValue(int value) => new AmplifyRune(value);
    public override string ValueLabel => $"+{Value}";
    protected override int Apply(PayloadRune rune, int value) => value + Value;
}

public sealed class TwinRune(int factor = 2) : ModifierRune(factor)
{
    public override string Key => "TWIN";
    public override Rune WithValue(int value) => new TwinRune(value);
    public override string ValueLabel => $"×{Value}";
    protected override int Apply(PayloadRune rune, int value) => value * Value;
}

/// <summary>Cancels Blood runes in the next glyph (or a whole loop body).</summary>
public sealed class SanctifyRune() : ModifierRune(0)
{
    public override string Key => "SANCTIFY";
    public override bool ShowsValue => false;
    protected override int Apply(PayloadRune rune, int value) => rune is BloodRune ? 0 : value;
}

public sealed class EchoRune(int count = 1) : ModifierRune(count)
{
    public override string Key => "ECHO";
    public override bool ShowsValue => Value > 1;
    public override string ValueLabel => $"+{Value}";
    public override int ExtraExecutions => Value;
    public override Rune WithValue(int value) => new EchoRune(value);
}

/// <summary>Curse-like modifier: the next rune (or a whole loop body) fizzles instead of resolving.</summary>
public sealed class VoidRune() : ModifierRune(0)
{
    public override string Key => "VOID";
    public override bool ShowsValue => false;
    public override bool Voids => true;
}

/// <summary>
/// Delays the next glyph: the Growth ticks down once on every trigger (so a Loop ticks it once per iteration) and
/// the grown glyph is kept for next turn with its values doubled instead of resolving. Ticked down to 0, the
/// Growth vanishes and the glyph resolves on its next trigger. Handled by the interpreter, not through <see cref="ModifierRune.ApplyTo"/>.
/// </summary>
public sealed class GrowthRune(int value) : ModifierRune(value)
{
    public override string Key => "GROWTH";
    public override bool Amplifiable => true;
    public override Rune WithValue(int value) => new GrowthRune(value);
}

/// <summary>
/// Sends the Speak back the way it came: earlier glyphs are Spoken again in reverse order, and glyphs after this
/// one are never Spoken. Handled by the interpreter.
/// </summary>
public sealed class ReflectionRune() : ModifierRune(0)
{
    public override string Key => "REFLECTION";
    public override bool ShowsValue => false;
}

/// <summary>
/// Persists (its card inscribes it with <see cref="Glyph.Persist"/>). Each trigger keeps a copy of the following
/// glyph for next turn; the copy persists too. Handled by the interpreter.
/// </summary>
public sealed class CloneRune() : ModifierRune(0)
{
    public override string Key => "CLONE";
    public override bool ShowsValue => false;
}

/// <summary>
/// Co-op: supportive (ally) payloads after this rune affect every living player. Value 0 = only the next rune;
/// any higher value = every following rune. Handled by the interpreter.
/// </summary>
public sealed class FriendshipRune(int value = 0) : ModifierRune(value)
{
    public override string Key => "FRIENDSHIP";
    public override bool ShowsValue => false;
    public override IEnumerable<IHoverTip> HoverTips => Value > 0
        ? [new HoverTip(
            new LocString(RuneTips.Table, LocKey + "_ALL.title"),
            new LocString(RuneTips.Table, LocKey + "_ALL.description"))]
        : base.HoverTips;
}
