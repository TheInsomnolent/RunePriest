using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>Ascended Attack: Inscribe a Strike, plus 3 additional damage for every rune currently inscribed.</summary>
public sealed class LightBlade() : RuneCard(0, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("Strike", 9m, ValueProp.Move),
        new IntVar("BonusPerRune", 3m)
    ];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new StrikeRune(Var("Strike") + IncantationRuneCount * DynamicVars["BonusPerRune"].IntValue))
            .AnchoredTo(anchor)
    ];

    /// <summary>Ascended cards never upgrade.</summary>
    public override int MaxUpgradeLevel => 0;

    protected override void OnUpgrade()
    {
    }
}
