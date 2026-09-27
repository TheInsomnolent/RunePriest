using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;

public sealed class NovaSigil() : RuneCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new TargetRune(TargetMode.Nova))];

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
