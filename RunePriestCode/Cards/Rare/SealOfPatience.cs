using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;

public sealed class SealOfPatience() : RuneCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new SealRune())];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
