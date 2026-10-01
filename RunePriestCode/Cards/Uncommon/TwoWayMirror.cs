using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>
/// Multiplayer only. Inscribe Reflection, and slip another Reflection into a random slot of a random ally's
/// Incantation (upgraded: at the end instead of a random slot).
/// </summary>
public sealed class TwoWayMirror() : RuneCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new ReflectionRune())];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);

        var others = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != Owner.Creature && c.IsAlive && c.IsPlayer && c.Player != null)
            .Select(c => c.Player!)
            .ToList();
        if (others.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatTargets;
        var ally = rng.NextItem(others)!;
        int? index = IsUpgraded ? null : rng.NextInt((RuneCmd.GetBuffer(ally.Creature)?.Glyphs.Count ?? 0) + 1);
        await RuneCmd.ShareAt(choiceContext, ally, Glyph.Of(new ReflectionRune()), index, this);
    }
}
