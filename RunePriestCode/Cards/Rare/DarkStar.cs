using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;
/// <summary>
/// Inscribe a persistent Void that swallows a rune every turn, and draw cards each turn while a Void is inscribed.
/// </summary>
public sealed class DarkStar() : RuneCard(0, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [RunePriestKeywords.Persist];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DarkStarPower>(1m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DarkStarPower>()];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new VoidRune()).Persist()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);
        await PowerCmd.Apply<DarkStarPower>(choiceContext, Owner.Creature,
            DynamicVars["DarkStarPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["DarkStarPower"].UpgradeValueBy(1m);
}
