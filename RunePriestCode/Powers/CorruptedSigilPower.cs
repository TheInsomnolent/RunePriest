using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Corrupted Sigil: cards with "Rune" in their name cost 0, Exhaust and (until an upgraded Corrupted Sigil is played)
/// are Ethereal. The Incantation is kept after being Spoken.
/// </summary>
public sealed class CorruptedSigilPower : RunePriestPower, IRuneListener
{
    private const string EtherealVar = "Ethereal";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(EtherealVar, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal), HoverTipFactory.FromKeyword(CardKeyword.Exhaust), RuneTips.Speak
    ];

    public bool MakesEthereal => DynamicVars[EtherealVar].BaseValue > 0;

    /// <summary>An upgraded Corrupted Sigil was played: rune cards stop being Ethereal.</summary>
    public void RemoveEthereal() => DynamicVars[EtherealVar].BaseValue = 0;

    public bool KeepsIncantation => true;

    private bool Affects(CardModel card) => card.Owner == Owner.Player && RunePriestCard.HasRuneInName(card);

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!Affects(card) || originalCost <= 0) return false;
        modifiedCost = 0;
        return true;
    }

    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (!Affects(card)) return false;
        var changed = keywords.Add(CardKeyword.Exhaust);
        if (MakesEthereal) changed |= keywords.Add(CardKeyword.Ethereal);
        return changed;
    }
}
