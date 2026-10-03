using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Ascended Skill - Cost 1 (Persist - card is playable again this turn)
/// Inscribe Hex.
/// </summary>
public sealed class Curse() : RuneCard(1, CardType.Skill, CardRarity.Ascended, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [RunePriestKeywords.Persist];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Hex", 1m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new HexRune(Var("Hex")))];
}
