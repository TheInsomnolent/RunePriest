using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>Starter: at end of turn, if the Incantation is empty, Inscribe Ward.</summary>
public sealed class ChalkStylus : RunePriestRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Ward", 4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe, ..new BlockRune(0).HoverTips];

    // Early phase so the Incantation (spoken in BeforeSideTurnEnd) includes the new glyph.
    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature)) return;
        if (RuneCmd.GetBuffer(Owner.Creature)?.Glyphs.Count > 0) return;

        Flash();
        await RuneCmd.Inscribe(choiceContext, Owner, [Glyph.Of(new BlockRune(DynamicVars["Ward"].IntValue))], null);
    }
}
