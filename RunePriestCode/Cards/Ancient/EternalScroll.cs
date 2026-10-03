using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Eternal Scroll: Ancient Skill, Cost 0
/// May be etched at rest sites to bind runes permanently.
/// Inscribe all etched runes when played.
/// Upgradable: Cost -1 per upgrade.
/// 
/// TODO: This is a stub pending rest-site integration.
/// - Requires event listeners or Harmony patches to intercept rest-site choices
/// - Complex integration deferred to follow-up task
/// </summary>
public sealed class EternalScroll() : RunePriestCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // TODO: Inscribe all etched runes from this card's collection
        MainFile.Logger.Info("[Rune] EternalScroll: played (etching integration not yet implemented)");
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        // Cost -1 per upgrade
        EnergyCost.UpgradeBy(-1);
    }
}
