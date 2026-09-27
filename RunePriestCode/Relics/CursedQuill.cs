using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Cards.Status;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>Strike and Ward runes gain a bonus. Upon pickup, add a Stray Glyph curse to your deck.</summary>
public sealed class CursedQuill : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool HasUponPickupEffect => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Bonus", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [..new StrikeRune(0).HoverTips, ..new BlockRune(0).HoverTips, ..HoverTipFactory.FromCardWithCardHoverTips<StrayRune>()];

    public int ModifyRuneValue(RuneContext ctx, PayloadRune rune, int value) =>
        rune is StrikeRune or BlockRune ? value + DynamicVars["Bonus"].IntValue : value;

    public override async Task AfterObtained()
    {
        await CardPileCmd.AddCurseToDeck<StrayRune>(Owner);
    }
}
