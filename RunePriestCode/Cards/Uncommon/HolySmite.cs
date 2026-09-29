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
/// <summary>Imbue, then hit harder for each rune Imbued into this card.</summary>
public sealed class HolySmite() : RunePriestCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(11m, ValueProp.Move), new IntVar("Imbue", 2m), new DamageVar("Bonus", 5m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => ImbueHoverTips;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Imbue(choiceContext, cardPlay, DynamicVars["Imbue"].IntValue);
        var imbued = ImbuedGlyphs.Count;
        await DealDamage(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + imbued * DynamicVars["Bonus"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Bonus"].UpgradeValueBy(1m);
    }
}
