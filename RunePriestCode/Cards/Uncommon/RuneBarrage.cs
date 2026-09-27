using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

public sealed class RuneBarrage() : RuneCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
{
    private const int Hits = 4;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 4m, ValueProp.Move), new IntVar("Hits", Hits)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new TargetRune(TargetMode.Chaos)), ..Enumerable.Range(0, Hits).Select(_ => Glyph.Of(new StrikeRune(Var("Strike"))))];

    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(1m);
}
