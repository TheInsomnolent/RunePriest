using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>
/// Ancient (offered by Vakuu, see <c>AncientOptionPatches</c>): every [Void] you Inscribe Persists, and whenever one of
/// your Voids consumes a rune, deal damage equal to the consumed rune's value (the sum of its shown values, e.g.
/// Strike 14 + Blood 3 = 17; valueless runes such as Loop deal nothing) to ALL enemies.
/// </summary>
public sealed class EventHorizon : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [..new VoidRune().HoverTips, HoverTipFactory.FromKeyword(RunePriestKeywords.Persist)];

    private static bool IsVoid(Glyph glyph) => glyph is { Kind: RuneKind.Modifier, Runes: [VoidRune] };

    /// <summary>The value a consumed rune deals: every value it shows (Twin ×2 → 2, Amplify +4 → 4).</summary>
    public static int ValueOf(Glyph glyph) => glyph.Runes.Where(r => r.ShowsValue).Sum(r => Math.Max(0, r.Value));

    public IReadOnlyList<Glyph> ModifyInscription(Player player, IReadOnlyList<Glyph> glyphs, bool preview) =>
        player != Owner || !glyphs.Any(g => IsVoid(g) && !g.Persistent)
            ? glyphs
            : glyphs.Select(g => IsVoid(g) && !g.Persistent ? g.Persist() : g).ToList();

    public async Task AfterVoided(RuneContext ctx, Glyph voider, Glyph consumed)
    {
        if (ctx.Owner != Owner || Owner.Creature.CombatState is not { } combat) return;
        var damage = ValueOf(consumed);
        var enemies = combat.HittableEnemies.ToList();
        if (damage <= 0 || enemies.Count == 0) return;
        Flash();
        await CreatureCmd.Damage(ctx.ChoiceContext, enemies, damage, ValueProp.Unpowered, Owner.Creature);
    }
}
