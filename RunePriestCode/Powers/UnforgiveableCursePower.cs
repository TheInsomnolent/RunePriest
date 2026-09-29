using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;
using RunePriest.RunePriestCode.Cards.Special;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Whenever a rune fizzles, add Amount random cursed weapons (Cursed Sword/Armour/Spirits) to your hand.
/// Fizzles while Speaking at end of turn deliver their weapons at the start of your next turn.
/// </summary>
public sealed class UnforgiveableCursePower : RunePriestPower, IRuneListener
{
    private int _pending;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public static IEnumerable<IHoverTip> WeaponTips =>
    [
        HoverTipFactory.FromCard<CursedSword>(),
        HoverTipFactory.FromCard<CursedArmour>(),
        HoverTipFactory.FromCard<CursedSpirits>()
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [..base.ExtraHoverTips, ..WeaponTips];

    public async Task AfterFizzle(RuneContext ctx, Glyph glyph)
    {
        Flash();
        if (ctx.Timing == SpeakTiming.EndOfTurn)
            _pending += Amount;
        else
            await Summon(Amount);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || _pending <= 0) return;
        var count = _pending;
        _pending = 0;
        await Summon(count);
    }

    private async Task Summon(int count)
    {
        var player = Owner.Player;
        var combat = Owner.CombatState;
        if (player == null || combat == null) return;

        for (var i = 0; i < count; i++)
        {
            CardModel card = player.RunState.Rng.CombatCardGeneration.NextInt(3) switch
            {
                0 => combat.CreateCard<CursedSword>(player),
                1 => combat.CreateCard<CursedArmour>(player),
                _ => combat.CreateCard<CursedSpirits>(player)
            };
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player));
        }
    }
}
