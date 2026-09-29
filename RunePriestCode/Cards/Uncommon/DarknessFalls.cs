using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>Inscribe Void, then a huge Strike. The Void swallows the Strike unless something gets between them.</summary>
public sealed class DarknessFalls() : RuneCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar("Strike", 30m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new VoidRune()), Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor)];

    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(10m);
}
