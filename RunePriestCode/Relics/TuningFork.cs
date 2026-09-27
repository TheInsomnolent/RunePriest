using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Powers;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>Start each combat with Resonance.</summary>
public sealed class TuningFork : RunePriestRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ResonancePower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ResonancePower>()];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || player.Creature.CombatState?.RoundNumber != 1) return;
        Flash();
        await PowerCmd.Apply<ResonancePower>(choiceContext, player.Creature,
            DynamicVars["ResonancePower"].BaseValue, player.Creature, null);
    }
}
