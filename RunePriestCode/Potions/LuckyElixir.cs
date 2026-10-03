using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Potions;

/// <summary>Inscribes a single Swift + Mend rune.</summary>
public sealed class LuckyElixir : RunePriestPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Swift", 2m), new IntVar("Mend", 2m)];
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        [RuneTips.Inscribe, ..new SwiftRune(0).HoverTips, ..new MendRune(0).HoverTips];

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var glyph = Glyph.Of(new SwiftRune(DynamicVars["Swift"].IntValue), new MendRune(DynamicVars["Mend"].IntValue));
        return RuneCmd.Inscribe(choiceContext, target?.Player ?? Owner, [glyph], null);
    }
}
