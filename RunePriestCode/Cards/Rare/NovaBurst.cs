using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;

public sealed class NovaBurst() : RuneCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 8m, ValueProp.Move), new IntVar("Twin", 2m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new TargetRune(TargetMode.Nova)),
        Glyph.Of(new TwinRune(Var("Twin"))),
        Glyph.Of(new StrikeRune(Var("Strike")))
    ];

    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(3m);
}
