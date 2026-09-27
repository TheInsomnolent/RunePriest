using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>+1 max Energy, but the Incantation holds at most Capacity glyphs (overflow is Spoken immediately).</summary>
public sealed class SlateTablet : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new IntVar("Capacity", 5m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this), RuneTips.Overflow];

    public override decimal ModifyMaxEnergy(Player player, decimal amount) =>
        player == Owner ? amount + DynamicVars.Energy.IntValue : amount;

    public int? ModifyCapacity(int? capacity) => RuneListeners.Tighten(capacity, DynamicVars["Capacity"].IntValue);
}
