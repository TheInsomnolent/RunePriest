using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Cards.Special;
using RunePriest.RunePriestCode.Powers;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>Shop relic: cursed items (Cursed Sword, Cursed Armour, Cursed Spirits) cost 0.</summary>
public sealed class CursedRing : RunePriestRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override int MerchantCost => 200;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => UnforgiveableCursePower.WeaponTips;

    private static bool IsCursedItem(CardModel card) => card is CursedSword or CursedArmour or CursedSpirits;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner != Owner || card.EnergyCost.CostsX || originalCost <= 0 || !IsCursedItem(card)) return false;
        modifiedCost = 0;
        return true;
    }
}
