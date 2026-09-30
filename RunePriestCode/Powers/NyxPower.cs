using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>Whenever you play a card Imbued with a Void rune, deal Amount damage to ALL enemies.</summary>
public sealed class NyxPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (cardPlay.Card is not RunePriestCard { IsImbued: true } card) return;
        if (!card.ImbuedGlyphs.Any(g => g.Runes.Any(r => r is VoidRune))) return;

        var enemies = Owner.CombatState?.HittableEnemies.ToList();
        if (enemies == null || enemies.Count == 0) return;
        Flash();
        await CreatureCmd.Damage(choiceContext, enemies, Amount, ValueProp.Unpowered, Owner);
    }
}
