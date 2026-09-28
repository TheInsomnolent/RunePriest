using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
public sealed class NovaRuneCard() : RuneCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new TargetRune(TargetMode.Nova))];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
