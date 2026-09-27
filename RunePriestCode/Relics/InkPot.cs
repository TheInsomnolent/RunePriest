using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>Whenever you Speak N or more distinct glyphs at once, gain 1 Energy (next turn if at end of turn).</summary>
public sealed class InkPot : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Glyphs", 5m), new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Speak, HoverTipFactory.ForEnergy(this)];

    public async Task AfterSpeak(RuneContext ctx)
    {
        if (ctx.GlyphsSpoken < DynamicVars["Glyphs"].IntValue) return;
        Flash();
        var energy = DynamicVars.Energy.IntValue;
        if (ctx.Timing == SpeakTiming.Invoked)
            await PlayerCmd.GainEnergy(energy, Owner);
        else
            await PowerCmd.Apply<EnergyNextTurnPower>(ctx.ChoiceContext, Owner.Creature, energy, Owner.Creature, null);
    }
}
