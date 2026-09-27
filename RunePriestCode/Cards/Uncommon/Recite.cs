using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

/// <summary>Deal damage for each glyph in the Incantation.</summary>
public sealed class Recite() : RunePriestCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        DealDamage(choiceContext, cardPlay, DynamicVars.Damage.BaseValue * IncantationSize);

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
