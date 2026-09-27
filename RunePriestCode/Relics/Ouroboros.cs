using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

public sealed class Ouroboros : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Extra", 1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new LoopRune(0).HoverTips;

    public int ModifyLoopCount(RuneContext ctx, LoopRune loop, int count) =>
        count > 0 ? count + DynamicVars["Extra"].IntValue : count;
}
