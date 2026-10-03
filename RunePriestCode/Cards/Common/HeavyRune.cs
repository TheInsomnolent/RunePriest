using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;
using Ascended = RunePriest.RunePriestCode.Cards.Ancient;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Inscribe a Strike that grows with every rune already in the Incantation.</summary>
public sealed class HeavyRune() : RuneCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), ITranscendenceCard
{
    /// <summary>Archaic Tooth (Orobas) transforms this starting rune into its Ascended form.</summary>
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<Ascended.LightBlade>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 9m, ValueProp.Move), new IntVar("Bonus", 3m)];

    // Glyphs(null) is only used for hover tips; the real value is read when played.
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await RuneCmd.Inscribe(choiceContext, Owner, Inscription(cardPlay.Target), this);

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor) => draft.Inscribe(Inscription(anchor), this);

    private Glyph[] Inscription(Creature? anchor) =>
        [Glyph.Of(new StrikeRune(Var("Strike") + IncantationRuneCount * Var("Bonus"))).AnchoredTo(anchor)];

    protected override void OnUpgrade() => DynamicVars["Bonus"].UpgradeValueBy(1m);
}
