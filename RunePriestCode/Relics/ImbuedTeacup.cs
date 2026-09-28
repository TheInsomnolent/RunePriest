using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>The first rune you Inscribe each turn has its values doubled.</summary>
public sealed class ImbuedTeacup : RunePriestRelic, IRuneListener
{
    private bool _usedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe];

    public override Task BeforeCombatStart()
    {
        _usedThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner) _usedThisTurn = false;
        return Task.CompletedTask;
    }

    public IReadOnlyList<Glyph> ModifyInscription(Player player, IReadOnlyList<Glyph> glyphs)
    {
        if (player != Owner || _usedThisTurn || glyphs.Count == 0) return glyphs;
        _usedThisTurn = true;
        Flash();
        return [glyphs[0].Scaled(2), ..glyphs.Skip(1)];
    }
}
