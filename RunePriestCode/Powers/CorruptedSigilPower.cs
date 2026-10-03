using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Corrupted Sigil: Power that modifies all rune cards globally.
/// When active, all rune cards become ethereal (discarded at turn end), cost 0, and runes persist across turns.
/// Upgraded version: remove ethereal (power persists).
/// 
/// NOTE: This is a stub. Full implementation requires one of:
/// 1. OnCardInstanceModify hook (if available in BaseLib/game API)
/// 2. Harmony patch to card cost/keyword getters
/// 3. Custom event listener on card play/add
/// 
/// Current implementation tracks the power presence; actual card modification is deferred.
/// </summary>
public sealed class CorruptedSigilPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return new HoverTip(
                new("static_hover_tips", "RUNEPRIEST-CORRUPTED_SIGIL.title"),
                new("static_hover_tips", "RUNEPRIEST-CORRUPTED_SIGIL.description"));
            yield break;
        }
    }

    // TODO: Implement card modification hooks
    // Hook into: card.GetCost(), card.GetKeywords(), or card.OnAdd()
    // When a rune card enters the player's hand or is about to be played:
    // - Set cost to 0
    // - Add Ethereal keyword (if not upgraded)
    // - Mark runes to persist at turn end
}
