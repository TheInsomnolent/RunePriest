using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>At the start of combat, Inscribe Defend 10.</summary>
public sealed class AnchorScroll : RunePriestRelic
{
    private bool _used;

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar("Defend", 10m, ValueProp.Unpowered)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe, ..new DefendRune(0).HoverTips];

    public override Task BeforeCombatStart()
    {
        _used = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || _used) return;
        _used = true;
        Flash();
        await RuneCmd.Inscribe(choiceContext, Owner,
            [Glyph.Of(new DefendRune(DynamicVars["Defend"].IntValue))], null);
    }
}
