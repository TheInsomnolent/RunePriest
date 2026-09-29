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
public sealed class EchoingWard() : RuneCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar("Defend", 8m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new DefendRune(Var("Defend"))).AnchoredTo(anchor), Glyph.Of(new EchoRune())];

    protected override void OnUpgrade() => DynamicVars["Defend"].UpgradeValueBy(3m);
}
