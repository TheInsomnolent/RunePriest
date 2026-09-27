using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;

/// <summary>Deal damage equal to the total base value of Strike runes in the Incantation.</summary>
public sealed class FinalWord() : RunePriestCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new StrikeRune(0).HoverTips;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var total = Incantation?.Glyphs.SelectMany(g => g.Runes).OfType<StrikeRune>().Sum(r => r.Value) ?? 0;
        return DealDamage(choiceContext, cardPlay, total);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
