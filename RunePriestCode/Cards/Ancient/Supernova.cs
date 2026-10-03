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
public sealed class Supernova() : RuneCard(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new TargetRune(TargetMode.Nova))];

    /// <summary>Ascended cards never upgrade.</summary>
    public override int MaxUpgradeLevel => 0;

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);

        var buffer = Incantation;
        if (buffer == null) return;
        for (var i = buffer.Glyphs.Count - 1; i >= 0; i--)
        {
            if (i < buffer.Glyphs.Count && buffer.Glyphs[i].Runes.Any(r => r is VoidRune))
                await RuneCmd.Remove(choiceContext, Owner, i);
        }
    }
}
