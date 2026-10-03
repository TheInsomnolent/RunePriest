using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;

/// <summary>DEPRECATED: This card was replaced by other common attack options.</summary>
public sealed class QuickJab() : RuneCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3m, ValueProp.Move), new DamageVar("Strike", 3m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DealDamage(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        await base.OnPlay(choiceContext, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["Strike"].UpgradeValueBy(1m);
    }
}
