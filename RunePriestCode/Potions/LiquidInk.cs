using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Potions;

public sealed class LiquidInk : RunePriestPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amplify", 5m)];
    public override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe, ..new AmplifyRune(0).HoverTips];

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        RuneCmd.Inscribe(choiceContext, target?.Player ?? Owner, [Glyph.Of(new AmplifyRune(DynamicVars["Amplify"].IntValue))], null);
}
