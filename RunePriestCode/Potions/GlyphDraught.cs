using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Potions;

/// <summary>Speak the target player's Incantation now.</summary>
public sealed class GlyphDraught : RunePriestPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;
    public override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Speak];

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        RuneCmd.Speak(choiceContext, target?.Player ?? Owner, SpeakTiming.Invoked);
}
