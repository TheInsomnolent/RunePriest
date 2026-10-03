using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Ascended Skill - Cost 1
/// Inscribe Twin 2 + Twin 2 (4x multiplier total).
/// </summary>
public sealed class Multiply() : RuneCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new TwinRune(2)),
        Glyph.Of(new TwinRune(2))
    ];

    protected override void OnUpgrade()
    {
        // Ascended cards never upgrade
    }
}
