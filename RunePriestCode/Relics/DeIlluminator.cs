using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>The first Void you Inscribe each combat immediately Fizzles.</summary>
public sealed class DeIlluminator : RunePriestRelic, IRuneListener
{
    private bool _used;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [RuneTips.Inscribe, RuneTips.Fizzle, ..new VoidRune().HoverTips];

    public override Task BeforeCombatStart()
    {
        _used = false;
        return Task.CompletedTask;
    }

    public async Task AfterInscribed(PlayerChoiceContext choiceContext, Player player, IReadOnlyList<Glyph> glyphs)
    {
        if (player != Owner || _used) return;
        if (!glyphs.Any(g => g.Runes.Any(r => r is VoidRune))) return;
        var buffer = RuneCmd.GetBuffer(Owner.Creature);
        if (buffer == null) return;

        // Newest first: merging means the buffer glyph may not be the same object as the inscribed one.
        for (var i = buffer.Glyphs.Count - 1; i >= 0; i--)
        {
            if (!buffer.Glyphs[i].Runes.Any(r => r is VoidRune)) continue;
            _used = true;
            Flash();
            await RuneCmd.Fizzle(choiceContext, Owner, i);
            return;
        }
    }
}
