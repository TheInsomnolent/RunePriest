using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Ascended Skill - Cost 1
/// Inscribe Nova. Remove all void in your incantation.
/// </summary>
public sealed class Supernova() : RuneCard(1, CardType.Skill, CardRarity.Ascended, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new TargetRune(TargetMode.Nova))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);
        
        // Clear all void runes from the buffer
        var buffer = Incantation;
        if (buffer != null)
        {
            for (int i = buffer.Glyphs.Count - 1; i >= 0; i--)
            {
                var glyph = buffer.Glyphs[i];
                if (glyph.Runes.Any(r => r is VoidRune))
                {
                    buffer.RemoveAt(i);
                }
            }
        }
    }
}
