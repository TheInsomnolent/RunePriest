using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>Inscribe Kindle equal to the number of runes currently in the Incantation (simultaneous runes each count).</summary>
public sealed class Candlelight() : RuneCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // Glyphs(null) is only used for hover tips; the real count is read when played.
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new KindleRune(1))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await RuneCmd.Inscribe(choiceContext, Owner, Inscription, this);

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor) => draft.Inscribe(Inscription, this);

    private Glyph[] Inscription => IncantationRuneCount is var count and > 0 ? [Glyph.Of(new KindleRune(count))] : [];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
