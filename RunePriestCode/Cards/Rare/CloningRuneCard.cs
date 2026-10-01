using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;
/// <summary>Inscribe a persistent Clone, and this card clones itself into the discard pile too.</summary>
public sealed class CloningRuneCard() : RuneCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [RunePriestKeywords.Persist];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new CloneRune()).Persist()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);
        var copy = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(copy,
            PileType.Discard, Owner, CardPilePosition.Random));
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
