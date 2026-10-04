using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards;

/// <summary>
/// <c>{Inscribed}</c>: the Strike a card would Inscribe if played now, for cards whose Strike depends on the combat
/// (Heavy Rune's bonus per rune). Card text shows it in combat like the game's calculated damage:
/// <c>{InCombat:\n(Inscribes [blue]Strike[/blue] {Inscribed:diff()})|}</c>. In hand it previews like the Strike rune
/// itself (Strength, Weak, the targeted enemy's Vulnerable…); <paramref name="value"/> already includes the card's
/// enchantment (<see cref="RuneCard"/>'s <c>Var</c>), so it isn't applied again.
/// </summary>
/// <param name="value">The inscribed value; must be static (vars are shared with the card's copies).</param>
public sealed class InscribedStrikeVar(Func<CardModel, int> value) : DynamicVar(VarName, 0m)
{
    public const string VarName = "Inscribed";

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        var inscribed = value(card);
        // Also the value shown when the preview is cleared without being recomputed.
        BaseValue = inscribed;
        if (!runGlobalHooks) return;

        var strike = new StrikeRune(inscribed);
        var glyph = Glyph.Of(strike).AnchoredTo(target).WithSource(card);
        PreviewValue = Math.Max(0, strike.Modified(card.Owner, glyph, target, inscribed));
    }
}
