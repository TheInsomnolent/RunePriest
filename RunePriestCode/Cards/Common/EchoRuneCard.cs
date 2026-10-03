using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Runes;
using Ascended = RunePriest.RunePriestCode.Cards.Ancient;

namespace RunePriest.RunePriestCode.Cards.Common;

public sealed class EchoRuneCard() : RuneCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), ITranscendenceCard
{
    /// <summary>Archaic Tooth (Orobas) transforms this starting rune into its Ascended form.</summary>
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<Ascended.Cathedral>();

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new EchoRune())];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
