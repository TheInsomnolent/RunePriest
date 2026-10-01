namespace RunePriest.RunePriestCode.Runes;

public abstract class FlowRune(int value = 0) : Rune(value)
{
    public override RuneKind Kind => RuneKind.Flow;
}

/// <summary>
/// Runs its body once, then <see cref="Rune.Value"/> extra times (Loop 1 = twice). The repeat count is fixed when
/// inscribed: Loops never merge and no value effect (Amplify, Twin, Growth, Empower, Scaled) changes it — modifiers
/// before a Loop empower the body, never the repeat count (Echo still multiplies the repetitions).
/// </summary>
public sealed class LoopRune(int count) : FlowRune(count)
{
    public override string Key => "LOOP";
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
