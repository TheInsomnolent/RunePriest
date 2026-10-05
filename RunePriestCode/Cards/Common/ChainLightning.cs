using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Scatter comes first so the card's own Strikes jump between random enemies.</summary>
public sealed class ChainLightning() : RuneCard(2, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{
    private const int Hits = 4;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 3m, ValueProp.Move), new IntVar("Hits", Hits)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new TargetRune(TargetMode.Scatter)), ..Enumerable.Range(0, Hits).Select(_ => Glyph.Of(new StrikeRune(Var("Strike"))))];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
