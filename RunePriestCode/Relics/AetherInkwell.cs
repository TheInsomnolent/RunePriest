using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Potions;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>
/// Neow starting reward (see <see cref="Patches.NeowAetherQuillPatch"/>): upon pickup, obtain an Aether Quill.
/// Ancient rarity, so it never shows up in regular relic rewards.
/// </summary>
public sealed class AetherInkwell : RunePriestRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPotion<AetherQuill>(), RuneTips.Imbue];

    public override async Task AfterObtained()
    {
        await PotionCmd.TryToProcure<AetherQuill>(Owner);
    }
}
