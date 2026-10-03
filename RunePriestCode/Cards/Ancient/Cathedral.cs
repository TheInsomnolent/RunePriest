using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Cathedral: Ascended Skill, Cost 1
/// Inscribe two flow/modifier runes that control program execution:
/// - CloneRune: Persists a copy of the next glyph for next turn
/// - EchoRune: Executes the next glyph extra times
/// Together they enable powerful combo patterns.
/// Not upgradable (Ascended).
/// </summary>
public sealed class Cathedral() : RuneCard(1, CardType.Skill, CardRarity.Ascended, TargetType.Self)
{
    public override bool CanUpgrade => false;

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new CloneRune()),
        Glyph.Of(new EchoRune())
    ];
}
