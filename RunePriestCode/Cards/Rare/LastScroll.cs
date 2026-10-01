using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;
/// <summary>
/// Wraps the first rune of your Incantation in a closed Loop, removes every other rune, and seals your quill:
/// you may no longer Inscribe this turn.
/// </summary>
public sealed class LastScroll() : RunePriestCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Loop", 5m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        RuneTips.Inscribe, ..new LoopRune(1).HoverTips, ..new EndLoopRune().HoverTips,
        HoverTipFactory.FromPower<LastScrollPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IncantationSize > 0)
        {
            while (IncantationSize > 1) await RuneCmd.Remove(choiceContext, Owner, 1);
            await RuneCmd.Inscribe(choiceContext, Owner, [Glyph.Of(new EndLoopRune())], this);
            await RuneCmd.Prepend(choiceContext, Owner, [Glyph.Of(new LoopRune(Var("Loop")))], this);
        }
        await PowerCmd.Apply<LastScrollPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    private int Var(string name) => DynamicVars[name].IntValue;

    protected override void OnUpgrade() => DynamicVars["Loop"].UpgradeValueBy(3m);
}
