using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;
using Ascended = RunePriest.RunePriestCode.Cards.Ancient;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Inscribe several small simultaneous Strikes (one compound glyph: they resolve together).</summary>
public sealed class CrackedRuneCard() : RuneCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), ITranscendenceCard
{
    /// <summary>Archaic Tooth (Orobas) transforms this starting rune into its Ascended form.</summary>
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<Ascended.Disintegrate>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 2m, ValueProp.Move), new IntVar("Hits", 2m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(Enumerable.Range(0, Var("Hits")).Select(_ => (Rune)new StrikeRune(Var("Strike"))).ToArray()).AnchoredTo(anchor)];

    protected override void OnUpgrade() => DynamicVars["Hits"].UpgradeValueBy(1m);
}
