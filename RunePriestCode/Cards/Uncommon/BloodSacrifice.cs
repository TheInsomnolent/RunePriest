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
/// <summary>X cost: Inscribe Blood 1 X times, then Strike X X times (upgraded: Echo, then Strike X+1, X+1 times).</summary>
public sealed class BloodSacrifice() : RunePriestCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        RuneTips.Inscribe,
        ..new BloodRune(1).HoverTips,
        ..(IsUpgraded ? new EchoRune().HoverTips : Array.Empty<IHoverTip>()),
        ..new StrikeRune(0).HoverTips
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await RuneCmd.Inscribe(choiceContext, Owner, Inscription(ResolveEnergyXValue(), cardPlay.Target), this);

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor) =>
        draft.Inscribe(Inscription(PreviewXValue, anchor), this);

    private List<Glyph> Inscription(int x, Creature? anchor)
    {
        var strikes = IsUpgraded ? x + 1 : x;
        if (strikes <= 0) return [];

        return Enumerable.Range(0, x).Select(_ => Glyph.Of(new BloodRune(1)))
            .Concat(IsUpgraded ? [Glyph.Of(new EchoRune())] : Array.Empty<Glyph>())
            .Concat(Enumerable.Range(0, strikes).Select(_ => Glyph.Of(new StrikeRune(strikes)).AnchoredTo(anchor)))
            .ToList();
    }
}
