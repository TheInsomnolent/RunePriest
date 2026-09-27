using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

public sealed class ResoundingSign() : RuneCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new EchoRune()), Glyph.Of(new EchoRune())];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
