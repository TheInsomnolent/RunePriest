using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;
/// <summary>Whenever a rune fizzles, add a random cursed weapon to your hand. Upgraded: also Inscribe Nova 3 times.</summary>
public sealed class UnforgiveableCurse() : RunePriestCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Novas", 3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<UnforgiveableCursePower>(),
        RuneTips.Fizzle,
        ..UnforgiveableCursePower.WeaponTips,
        ..(IsUpgraded ? [RuneTips.Inscribe, ..new TargetRune(TargetMode.Nova).HoverTips] : Array.Empty<IHoverTip>())
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<UnforgiveableCursePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        if (IsUpgraded) await RuneCmd.Inscribe(choiceContext, Owner, Novas, this);
    }

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor)
    {
        if (IsUpgraded) draft.Inscribe(Novas, this);
    }

    private IEnumerable<Glyph> Novas =>
        Enumerable.Range(0, DynamicVars["Novas"].IntValue).Select(_ => Glyph.Of(new TargetRune(TargetMode.Nova)));
}
