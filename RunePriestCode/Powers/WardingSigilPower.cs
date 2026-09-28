using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>After you Speak, gain Amount Block for each rune Spoken.</summary>
public sealed class WardingSigilPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterSpeak(RuneContext ctx)
    {
        if (ctx.GlyphsSpoken == 0) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount * ctx.GlyphsSpoken, ValueProp.Unpowered, null);
    }
}
