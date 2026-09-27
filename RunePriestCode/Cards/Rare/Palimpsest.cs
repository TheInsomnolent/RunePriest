using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;

/// <summary>Inscribe a copy of every glyph currently in the Incantation.</summary>
public sealed class Palimpsest() : RunePriestCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var copies = Incantation?.Glyphs.Select(g => g.Copy()).ToList() ?? [];
        await RuneCmd.Inscribe(choiceContext, Owner, copies, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
