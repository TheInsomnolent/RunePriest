using MegaCrit.Sts2.Core.Entities.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>Incantation capped at <see cref="Capacity"/> glyphs (overflow is Spoken); Strike runes gain Amount.</summary>
public sealed class TightScriptPower : RunePriestPower, IRuneListener
{
    public const int Capacity = 4;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int? ModifyCapacity(int? capacity) => RuneListeners.Tighten(capacity, Capacity);

    public int ModifyRuneValue(RuneContext ctx, PayloadRune rune, int value) => rune is StrikeRune ? value + Amount : value;
}
