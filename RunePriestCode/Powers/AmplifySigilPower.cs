using MegaCrit.Sts2.Core.Entities.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>Every rune that can be amplified gains Amount.</summary>
public sealed class AmplifySigilPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int ModifyRuneValue(RuneContext ctx, PayloadRune rune, int value) => rune.Scalable ? value + Amount : value;
}
