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
/// <summary>X cost: Inscribe Blood 1 X times, then Strike X 2X times (upgraded: Strike 2X, 2X times).</summary>
public sealed class BloodSacrifice() : RunePriestCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        RuneTips.Inscribe,
        ..new BloodRune(1).HoverTips,
        ..new StrikeRune(0).HoverTips
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await RuneCmd.Inscribe(choiceContext, Owner, Inscription(ResolveEnergyXValue(), cardPlay.Target), this);

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor) =>
        draft.Inscribe(Inscription(PreviewXValue, anchor), this);

    private List<Glyph> Inscription(int x, Creature? anchor)
    {
        if (x <= 0) return [];
        var strike = IsUpgraded ? x * 2 : x;

        return Enumerable.Range(0, x).Select(_ => Glyph.Of(new BloodRune(1)))
            .Concat(Enumerable.Range(0, x * 2).Select(_ => Glyph.Of(new StrikeRune(strike)).AnchoredTo(anchor)))
            .ToList();
    }
}
