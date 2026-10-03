using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>
/// Blood inscriptions are doubled.
/// 
/// TODO (Future): "All Cards with Blood effects become free" requires a Harmony patch or event system
/// to intercept card cost calculations when cards are added to hand/drawn. Currently only implements
/// Blood value doubling via ModifyRuneValue hook.
/// </summary>
public sealed class LightweightClothRobe : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override int MerchantCost => 350;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [RuneTips.Inscribe, ..new BloodRune(0).HoverTips];

    /// <summary>Double Blood rune values when inscribed.</summary>
    public int ModifyRuneValue(RuneContext ctx, PayloadRune rune, int value)
    {
        if (ctx.Owner != Owner || rune is not BloodRune) return value;
        Flash();
        return value * 2;
    }
}

