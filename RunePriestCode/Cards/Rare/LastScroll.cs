using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;
/// <summary>
/// Wraps the first rune of your Incantation in nested closed Loops, removes every other rune, and seals your quill:
/// you may no longer Inscribe this turn.
/// </summary>
public sealed class LastScroll() : RunePriestCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Loops", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        RuneTips.Inscribe, ..new LoopRune().HoverTips, ..new EndLoopRune().HoverTips,
        HoverTipFactory.FromPower<LastScrollPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IncantationSize > 0)
        {
            var loops = DynamicVars["Loops"].IntValue;
            while (IncantationSize > 1) await RuneCmd.Remove(choiceContext, Owner, 1);
            await RuneCmd.Inscribe(choiceContext, Owner, Enumerable.Range(0, loops).Select(_ => Glyph.Of(new EndLoopRune())).ToList(), this);
            await RuneCmd.Prepend(choiceContext, Owner, Enumerable.Range(0, loops).Select(_ => Glyph.Of(new LoopRune())).ToList(), this);
        }
        await PowerCmd.Apply<LastScrollPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Loops"].UpgradeValueBy(1m);
}
