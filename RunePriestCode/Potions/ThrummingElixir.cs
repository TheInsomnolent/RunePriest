using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Potions;
/// <summary>Adds a flat bonus to every rune already inscribed (not an Amplify rune, so Loops don't multiply it).</summary>
public sealed class ThrummingElixir : RunePriestPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amplify", 2m)];
    public override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe];

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var bonus = DynamicVars["Amplify"].IntValue;
        RuneCmd.Transform(target?.Player ?? Owner, g => g.CanEmpower ? g.Empower(bonus) : g);
        return Task.CompletedTask;
    }
}
