using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Powers;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Corrupted Sigil: Ancient Power, Cost 0
/// All rune cards become Ethereal and cost 0.
/// Runes persist between turns.
/// Upgradable: Remove Ethereal.
/// 
/// TODO: This is a stub pending card modification integration.
/// - Requires OnCardInstanceModify hook or Harmony patch to intercept card cost/keyword getters
/// - Complex integration deferred to follow-up task
/// </summary>
public sealed class CorruptedSigil() : RunePriestCard(0, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Apply the power that modifies rune cards
        await PowerCmd.Apply<CorruptedSigilPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // Upgrade: power still applies but doesn't make cards Ethereal
        // This would require passing info to the power or creating a different power variant
        // Deferred to implementation task
    }
}
