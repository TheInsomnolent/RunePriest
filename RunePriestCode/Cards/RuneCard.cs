using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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

    /// <summary>The runes this card inscribes, unanchored (e.g. to etch them onto an Eternal Scroll).</summary>
    public IReadOnlyList<Glyph> InscribedGlyphs => Glyphs(null).ToList();

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        RuneCmd.Inscribe(choiceContext, Owner, Glyphs(cardPlay.Target), this);

    /// <summary>
    /// A var's value, with this card's enchantment (Sharp, Nimble…) baked into damage and block values: the rune
    /// carries it, so it is amplified/looped with the rune and never re-applied when it resolves.
    /// </summary>
    protected int Var(string name)
    {
        var dynamicVar = DynamicVars[name];
        var value = dynamicVar.BaseValue;
        switch (Enchantment, dynamicVar)
        {
            case ({ } enchantment, DamageVar damage):
                value += enchantment.EnchantDamageAdditive(value, damage.Props);
                value *= enchantment.EnchantDamageMultiplicative(value, damage.Props);
                break;
            case ({ } enchantment, BlockVar):
                value += enchantment.EnchantBlockAdditive(value);
                value *= enchantment.EnchantBlockMultiplicative(value);
                break;
        }
        return (int)value;
    }
}
