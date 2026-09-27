using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>At the start of each combat, Inscribe Ward.</summary>
public sealed class WardingCharm : RunePriestRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Ward", 6m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe, ..new BlockRune(0).HoverTips];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || player.Creature.CombatState?.RoundNumber != 1) return;
        Flash();
        await RuneCmd.Inscribe(choiceContext, player, [Glyph.Of(new BlockRune(DynamicVars["Ward"].IntValue))], null);
    }
}
