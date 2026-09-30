using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>Inscribe Blood, then a Reflection that sends the Speak back the way it came.</summary>
public sealed class ReflectionRuneCard() : RuneCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Blood", 2m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new BloodRune(Var("Blood"))), Glyph.Of(new ReflectionRune())];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
