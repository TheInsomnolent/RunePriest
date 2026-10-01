using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>
/// The first time each combat you Inscribe a Blood rune, Inscribe a Mend equal to the Blood amount.
/// </summary>
public sealed class HealersScroll : RunePriestRelic, IRuneListener
{
    private bool _used;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [RuneTips.Inscribe, ..new BloodRune(0).HoverTips, ..new MendRune(0).HoverTips];

    public override Task BeforeCombatStart()
    {
        _used = false;
        return Task.CompletedTask;
    }

    public async Task AfterInscribed(PlayerChoiceContext choiceContext, Player player, IReadOnlyList<Glyph> glyphs)
    {
        if (player != Owner || _used) return;
        var blood = glyphs.SelectMany(g => g.Runes).OfType<BloodRune>().FirstOrDefault();
        if (blood == null) return;
        _used = true;
        Flash();
        await RuneCmd.Inscribe(choiceContext, Owner, [Glyph.Of(new MendRune(blood.Value))], null);
    }
}
