using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>Whenever a rune fizzles, gain Block.</summary>
public sealed class HolySparkler : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar("Block", 4m, ValueProp.Unpowered)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Speak, RuneTips.Fizzle];

    public async Task AfterFizzle(RuneContext ctx, Glyph glyph)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Block"].BaseValue, ValueProp.Unpowered, null);
    }
}
