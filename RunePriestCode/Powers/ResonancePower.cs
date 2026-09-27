using MegaCrit.Sts2.Core.Entities.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>+Amount to every Strike and Ward payload (the Rune Priest's Focus).</summary>
public sealed class ResonancePower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int ModifyRuneValue(RuneContext ctx, PayloadRune rune, int value) =>
        rune is StrikeRune or WardRune ? value + Amount : value;
}
