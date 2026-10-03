using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

public sealed class Emanate() : RuneCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Hex", 1m), new IntVar("Blood", 1m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [
            Glyph.Of(new TargetRune(TargetMode.Nova)),
            Glyph.Of(new HexRune(Var("Hex"))),
            Glyph.Of(new BloodRune(Var("Blood")))
        ];

    protected override void OnUpgrade() => DynamicVars["Hex"].UpgradeValueBy(1m);
}
