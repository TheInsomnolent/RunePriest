using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Potions;

/// <summary>
/// Event potion (Neow reward via <see cref="Relics.AetherInkwell"/>): choose an Imbued card in your hand; its Imbue
/// becomes permanent (written to the deck card and saved with the run).
/// </summary>
public sealed class AetherQuill : RunePriestPotion
{
    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    public override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Imbue];

    /// <summary>Only usable while an Imbued card that can keep its runes is in hand, so the potion isn't wasted.</summary>
    public override bool PassesCustomUsabilityCheck =>
        Owner?.PlayerCombatState?.Hand.Cards.Any(CanBind) ?? false;

    private static bool CanBind(CardModel card) => card is RunePriestCard { CanMakeImbuePermanent: true };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, CanBind, this);
        if (selected.FirstOrDefault() is RunePriestCard card)
            card.MakeImbuePermanent();
    }
}
