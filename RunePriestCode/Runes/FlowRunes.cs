namespace RunePriest.RunePriestCode.Runes;

public abstract class FlowRune(int value = 0) : Rune(value)
{
    public override RuneKind Kind => RuneKind.Flow;
}

/// <summary>
/// Runs its body twice. Valueless: Loops never merge and no value effect changes them — modifiers before a Loop
/// empower the body (Echo multiplies the repetitions). Nest Loops for more repeats.
/// </summary>
public sealed class LoopRune() : FlowRune(0)
{
    public override string Key => "LOOP";
    public override bool ShowsValue => false;
}

public sealed class EndLoopRune() : FlowRune(0)
{
    public override string Key => "END_LOOP";
    public override bool ShowsValue => false;
}

public sealed class SealRune() : FlowRune(0)
{
    public override string Key => "SEAL";
    public override bool ShowsValue => false;
}
