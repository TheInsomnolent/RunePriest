using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

public sealed class BloodChalice : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Reduction", 1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new BloodRune(0).HoverTips;

    public int ModifyRuneValue(RuneContext ctx, PayloadRune rune, int value) =>
        rune is BloodRune ? value - DynamicVars["Reduction"].IntValue : value;
}
