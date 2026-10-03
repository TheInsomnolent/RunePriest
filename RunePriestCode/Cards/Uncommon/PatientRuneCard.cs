using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>
/// Inscribe a persistent Strike worth 1 plus every rune inscribed so far this combat (value fixed when played).
/// </summary>
public sealed class PatientRuneCard() : RuneCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(RunePriestKeywords.Persist)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar("Strike", 1m, ValueProp.Move)];

    // Glyphs(null) is only used for hover tips; the real value is read when played.
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor).Persist()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = Var("Strike") + (Incantation?.RunesInscribed ?? 0);
        await RuneCmd.Inscribe(choiceContext, Owner,
            [Glyph.Of(new StrikeRune(amount)).AnchoredTo(cardPlay.Target).Persist()], this);
    }

    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(4m);
}
