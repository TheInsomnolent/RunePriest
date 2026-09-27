using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>After you Speak at least one glyph, gain Amount Resonance.</summary>
public sealed class ArcaneFlowPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterSpeak(RuneContext ctx)
    {
        if (ctx.GlyphsSpoken == 0) return;
        Flash();
        await PowerCmd.Apply<ResonancePower>(ctx.ChoiceContext, Owner, Amount, Owner, null);
    }
}
