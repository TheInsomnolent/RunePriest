using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;

/// <summary>
/// Mirrororrim: Mirror the last rune inscribed. This card reflects/duplicates rune behavior.
/// Added to deck by Dusty Tome ancient.
/// </summary>
public sealed class Mirrororrim() : RunePriestCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Get the last glyph from the rune buffer
        var buffer = RuneCmd.GetBuffer(Owner.Creature);
        if (buffer?.Glyphs.Count == 0 || buffer == null)
        {
            MainFile.Logger.Info("[Rune] Mirrororrim: no runes in buffer to mirror");
            return;
        }

        var lastGlyph = buffer.Glyphs[^1];
        // Duplicate the last glyph and inscribe it
        var mirrored = lastGlyph.Copy().WithSource(this);
        await RuneCmd.Inscribe(choiceContext, Owner, [mirrored], null);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
