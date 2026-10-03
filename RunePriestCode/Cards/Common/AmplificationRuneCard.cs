using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;
using Ascended = RunePriest.RunePriestCode.Cards.Ancient;

namespace RunePriest.RunePriestCode.Cards.Common;
public sealed class AmplificationRuneCard() : RuneCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), ITranscendenceCard
{
    /// <summary>Archaic Tooth (Orobas) transforms this starting rune into its Ascended form.</summary>
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<Ascended.Multiply>();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amplify", 4m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new AmplifyRune(Var("Amplify")))];

    protected override void OnUpgrade() => DynamicVars["Amplify"].UpgradeValueBy(2m);
}
