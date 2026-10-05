using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Event;
/// <summary>Event card: Inscribe Blood 2. Upgraded: Exhaust.</summary>
public sealed class BloodRuneCard() : RuneCard(1, CardType.Skill, CardRarity.Event, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Blood", 2m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new BloodRune(Var("Blood")))];

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Exhaust);
}
