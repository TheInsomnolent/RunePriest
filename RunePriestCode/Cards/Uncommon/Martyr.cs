using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>Blood, Mend, then a Reflection to Speak them again backwards. Upgraded: loses Exhaust.</summary>
public sealed class Martyr() : RuneCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Blood", 10m), new HealVar("Mend", 10m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new BloodRune(Var("Blood"))),
        Glyph.Of(new MendRune(Var("Mend"))).AnchoredTo(anchor),
        Glyph.Of(new ReflectionRune())
    ];

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
