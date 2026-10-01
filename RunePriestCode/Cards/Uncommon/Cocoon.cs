using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>Inscribe a Growth incubating a persistent Defend: it doubles while it waits, then blocks every turn.</summary>
public sealed class Cocoon() : RuneCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [RunePriestKeywords.Persist];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Growth", 2m), new BlockVar("Defend", 2m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new GrowthRune(Var("Growth"))).Persist(),
        Glyph.Of(new DefendRune(Var("Defend"))).AnchoredTo(anchor).Persist(),
        Glyph.Of(new VoidRune()).Persist()
    ];

    protected override void OnUpgrade() => DynamicVars["Defend"].UpgradeValueBy(1m);
}
