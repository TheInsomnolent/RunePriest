using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>Blood and Mend runes swap their effects when Spoken.</summary>
public sealed class UndeadQuill : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override int MerchantCost => 300;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [RuneTips.Speak, ..new BloodRune(0).HoverTips, ..new MendRune(0).HoverTips];

    // Must stay side-effect free: also used by the Forecast preview.
    public PayloadRune ReplacePayload(RuneContext ctx, PayloadRune rune) => rune switch
    {
        BloodRune blood => new MendRune(blood.Value),
        MendRune mend => new BloodRune(mend.Value),
        _ => rune
    };
}
