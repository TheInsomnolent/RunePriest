using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Overgrowth: Ascended Skill, Cost 0
/// Inscribe a single OvergrowthRune modifier with value 3.
/// Ascended rarity prevents upgrades automatically.
/// </summary>
public sealed class Overgrowth() : RuneCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Growth", 3m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new OvergrowthRune(Var("Growth")))];

    protected override void OnUpgrade()
    {
        // Ascended cards never upgrade
    }
}
