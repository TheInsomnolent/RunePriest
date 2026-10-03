using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Potions;

/// <summary>Duplicates the last rune in your Incantation (a separate copy right after it; it doesn't merge).</summary>
public sealed class Teardrop : RunePriestPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    public override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe];

    /// <summary>Only usable while there is a rune to duplicate, so the potion isn't wasted.</summary>
    public override bool PassesCustomUsabilityCheck =>
        Owner?.Creature is { } creature && RuneCmd.GetBuffer(creature) is { IsSpeaking: false, Glyphs.Count: > 0 };

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        RuneCmd.Duplicate(choiceContext, Owner);
}
