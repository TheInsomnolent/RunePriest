using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;

public sealed class SanctifySigil() : RuneCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new SanctifyRune())];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
