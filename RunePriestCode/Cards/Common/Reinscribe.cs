using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;

/// <summary>Inscribe a copy of the last glyph in your Incantation.</summary>
public sealed class Reinscribe() : RunePriestCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var last = Incantation?.Glyphs.LastOrDefault();
        if (last != null)
            await RuneCmd.Inscribe(choiceContext, Owner, [last.Copy()], this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
