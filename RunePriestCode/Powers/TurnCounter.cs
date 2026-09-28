namespace RunePriest.RunePriestCode.Powers;

/// <summary>Mutable per-power-instance counter, for use as <c>InitInternalData</c> (values reset per clone).</summary>
public sealed class TurnCounter
{
    public int Value { get; set; }
}
