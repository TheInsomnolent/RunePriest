namespace RunePriest.RunePriestCode.Runes;

public abstract class FlowRune(int value = 0) : Rune(value)
{
    public override RuneKind Kind => RuneKind.Flow;
}

public sealed class LoopRune(int count) : FlowRune(count)
{
    public override string Key => "LOOP";
    public override Rune WithValue(int value) => new LoopRune(value);
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
