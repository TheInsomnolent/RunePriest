using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>
/// Event relic: cards with Blood runes (inscribed or Imbued) cost 0, and your Blood runes are doubled.
/// </summary>
public sealed class LightweightClothRobe : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [..new BloodRune(0).HoverTips];

    private static bool HasBlood(IEnumerable<Glyph> glyphs) => glyphs.Any(g => g.Runes.Any(r => r is BloodRune));

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        // X-cost cards (Blood Sacrifice) pay with whatever Energy you have, so "free" doesn't apply.
        if (card.Owner != Owner || card.EnergyCost.CostsX || originalCost <= 0) return false;
        var blood = card is RunePriestCard rp && HasBlood(rp.ImbuedGlyphs) || card is RuneCard rune && HasBlood(rune.InscribedGlyphs);
        if (!blood) return false;
        modifiedCost = 0;
        return true;
    }

    /// <summary>Must stay side-effect free (the forecast calls it), so the flash happens in <see cref="AfterPayload"/>.</summary>
    public int ModifyRuneValue(RuneContext ctx, PayloadRune rune, int value) =>
        ctx.Owner == Owner && rune is BloodRune ? value * 2 : value;

    public Task AfterPayload(RuneContext ctx, PayloadRune rune, int value, IReadOnlyList<Creature> targets)
    {
        if (ctx.Owner == Owner && rune is BloodRune) Flash();
        return Task.CompletedTask;
    }
}
