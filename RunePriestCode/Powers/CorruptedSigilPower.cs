using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// CorruptedSigil Power: All rune cards are Ethereal and cost 0.
/// Runes persist between turns.
/// 
/// TODO: This is a stub. Full implementation requires:
/// - OnCardInstanceModify hook or Harmony patch to intercept card cost/keyword getters
/// - Patch must run after card is added to hand/draw pile
/// - Complex integration deferred to follow-up task
/// </summary>
public sealed class CorruptedSigilPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];
}
