using MegaCrit.Sts2.Core.Entities.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Every rune is Spoken as if an <see cref="AmplifyRune"/> of Amount came right before it. Applied before the
/// rune's own modifiers, so a later Twin doubles it too; runes that aren't amplifiable are unaffected.
/// </summary>
public sealed class AmplifySigilPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int ModifyRuneValue(RuneContext ctx, PayloadRune rune, int value) => new AmplifyRune(Amount).ApplyTo(rune, value);
}
