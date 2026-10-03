using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Atrophy: Ascended Attack, Cost 2
/// Inscribe a massive DiminishRune(80), then Exhaust this card.
/// Diminish runes persist and halve in value every time they're Spoken.
/// Ascended rarity prevents upgrades automatically.
/// </summary>
public sealed class Atrophy() : RuneCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Damage", 80m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new DiminishRune(Var("Damage"))).AnchoredTo(anchor)];

    protected override void OnUpgrade()
    {
        // Ascended cards never upgrade
    }
}
