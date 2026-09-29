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
