using MegaCrit.Sts2.Core.Entities.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>Your Incantation is no longer cleared after Speaking.</summary>
public sealed class EternalSigilPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public bool KeepsIncantation => true;
}
