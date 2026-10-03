using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Scatter comes first: the big Strike hits a random enemy, and later runes keep scattering.</summary>
public sealed class BlindRage() : RuneCard(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar("Strike", 15m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new TargetRune(TargetMode.Scatter)), Glyph.Of(new StrikeRune(Var("Strike")))];

    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(5m);
}
