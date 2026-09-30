using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>Gain Block for each Nova rune currently in the Incantation.</summary>
public sealed class StarShield() : RunePriestCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar("Block", 4m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new TargetRune(TargetMode.Nova).HoverTips;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var novas = Incantation?.Glyphs.Sum(g => g.Runes.Count(r => r is TargetRune { Mode: TargetMode.Nova })) ?? 0;
        if (novas == 0) return;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Block"].BaseValue * novas, ValueProp.Move, null);
    }

    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(1m);
}
