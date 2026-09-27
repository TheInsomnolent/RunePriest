using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

public sealed class WhetstoneRune : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Common;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Bonus", 1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new StrikeRune(0).HoverTips;

    public int ModifyRuneValue(RuneContext ctx, PayloadRune rune, int value) =>
        rune is StrikeRune ? value + DynamicVars["Bonus"].IntValue : value;
}
