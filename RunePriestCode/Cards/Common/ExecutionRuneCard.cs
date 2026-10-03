using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;
using Ascended = RunePriest.RunePriestCode.Cards.Ancient;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Execution comes first so the Strike itself hits the lowest-HP enemy.</summary>
public sealed class ExecutionRuneCard() : RuneCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), ITranscendenceCard
{
    /// <summary>Archaic Tooth (Orobas) transforms this starting rune into its Ascended form.</summary>
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<Ascended.Annihilation>();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar("Strike", 10m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new TargetRune(TargetMode.Execution)), Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor)];

    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(5m);
}
