using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Cards.Special;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>Empty the Incantation and turn every removed rune into Energy. Upgraded: also hands you an Old Lantern.</summary>
public sealed class RerouteEnergy() : RunePriestCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        IsUpgraded ? [HoverTipFactory.FromCard<OldLantern>(false)] : [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var runes = 0;
        while (await RuneCmd.Remove(choiceContext, Owner) is { } glyph)
            runes += glyph.Runes.Count;
        if (runes > 0) await PlayerCmd.GainEnergy(runes, Owner);
        if (IsUpgraded) await AddToHand<OldLantern>(1);
    }

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor) => draft.RemoveWhere(_ => true);
}
