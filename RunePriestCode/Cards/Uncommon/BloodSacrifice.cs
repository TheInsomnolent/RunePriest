using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>X cost: Inscribe Blood 1 X times, then Strike X X times (upgraded: Strike X+1, X+1 times).</summary>
public sealed class BloodSacrifice() : RunePriestCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [RuneTips.Inscribe, ..new BloodRune(1).HoverTips, ..new StrikeRune(0).HoverTips];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var x = ResolveEnergyXValue();
        var strikes = IsUpgraded ? x + 1 : x;
        if (strikes <= 0) return;

        var glyphs = Enumerable.Range(0, x).Select(_ => Glyph.Of(new BloodRune(1)))
            .Concat(Enumerable.Range(0, strikes).Select(_ => Glyph.Of(new StrikeRune(strikes)).AnchoredTo(cardPlay.Target)));
        await RuneCmd.Inscribe(choiceContext, Owner, glyphs, this);
    }
}
