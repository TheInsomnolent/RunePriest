using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Imbue, then hit for a base amount plus the total value of the runes bound to this card.</summary>
public sealed class ImbuedSword() : RunePriestCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Move), new IntVar("Imbue", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => ImbueHoverTips;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Imbue(choiceContext, cardPlay, DynamicVars["Imbue"].IntValue);
        var bonus = ImbuedGlyphs.Sum(g => g.Runes.Sum(r => r.Value));
        await DealDamage(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + bonus);
    }

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor) =>
        PreviewImbue(draft, anchor, DynamicVars["Imbue"].IntValue);

    protected override void OnUpgrade() => DynamicVars["Imbue"].UpgradeValueBy(1m);
}
