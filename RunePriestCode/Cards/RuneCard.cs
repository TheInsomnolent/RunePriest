using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards;

/// <summary>
/// Base for cards that Inscribe glyphs. Implement <see cref="Glyphs"/>; the card's target becomes the glyph anchor.
/// Hover tips for Inscribe and every rune used are added automatically.
/// </summary>
public abstract class RuneCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : RunePriestCard(cost, type, rarity, target)
{
    /// <param name="anchor">The played card's target, or null (also null when building hover tips).</param>
    protected abstract IEnumerable<Glyph> Glyphs(Creature? anchor);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        RuneTips.Inscribe,
        ..Glyphs(null).SelectMany(g => g.Runes).DistinctBy(r => r.Key).SelectMany(r => r.HoverTips),
        ..AdditionalHoverTips
    ];

    protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        RuneCmd.Inscribe(choiceContext, Owner, Glyphs(cardPlay.Target), this);

    protected int Var(string name) => DynamicVars[name].IntValue;
}
