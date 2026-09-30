using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;
/// <summary>
/// Gain Block (doubled while Imbued) and Imbue. Unlike other Imbued cards, playing it releases its runes: the
/// Imbue slot is emptied so it can be filled again.
/// </summary>
public sealed class ImbuedShield() : RunePriestCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar("Block", 9m, ValueProp.Move), new IntVar("Imbue", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => ImbueHoverTips;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var wasImbued = IsImbued;
        await CreatureCmd.GainBlock(Owner.Creature,
            DynamicVars["Block"].BaseValue * (wasImbued ? 2 : 1), ValueProp.Move, null);
        await Imbue(choiceContext, cardPlay, DynamicVars["Imbue"].IntValue);
        if (wasImbued) ImbuedRunes = "";
    }

    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(2m);
}
