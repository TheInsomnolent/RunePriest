using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Basic;

public sealed class EchoSign() : RuneCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new EchoRune())];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
