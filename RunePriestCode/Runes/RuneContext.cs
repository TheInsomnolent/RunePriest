using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace RunePriest.RunePriestCode.Runes;

public enum SpeakTiming
{
    EndOfTurn,
    Invoked
}

/// <summary>How long a Friendship rune keeps routing supportive payloads to every player.</summary>
public enum FriendshipScope
{
    None,
    NextGlyph,
    Rest
}

/// <summary>Mutable state for a single Speak (or a side-effect-free preview when <see cref="Preview"/> is set).</summary>
public sealed class RuneContext(PlayerChoiceContext choiceContext, Player owner, RuneBuffer buffer, SpeakTiming timing, RunePreview? preview = null)
{
    private readonly HashSet<Glyph> _spoken = [];

    public PlayerChoiceContext ChoiceContext { get; } = choiceContext;
    public Player Owner { get; } = owner;
    public RuneBuffer Buffer { get; } = buffer;
    public SpeakTiming Timing { get; } = timing;
    public RunePreview? Preview { get; } = preview;
    public bool IsPreview => Preview != null;
    public TargetMode TargetMode { get; private set; } = TargetMode.Anchor;
    public bool Mirrored { get; private set; }

    /// <summary>While set, supportive (ally) payloads reach every living player instead of just the caster.</summary>
    public FriendshipScope Friendship { get; private set; } = FriendshipScope.None;

    /// <summary>Distinct glyphs activated during this Speak.</summary>
    public int GlyphsSpoken => _spoken.Count;
    public int Fizzles { get; set; }

    public Creature Creature => Owner.Creature;
    public ICombatState CombatState => Creature.CombatState!;

    public bool ShouldStop => CombatManager.Instance.IsOverOrEnding || Creature.IsDead || Creature.CombatState == null;

    /// <summary>Snapshot, so listeners may add/remove powers while being iterated.</summary>
    public IReadOnlyList<IRuneListener> Listeners => RuneListeners.Of(Owner);

    public void MarkSpoken(Glyph glyph) => _spoken.Add(glyph);

    public IReadOnlyList<Creature> ResolveTargets(PayloadRune rune, Glyph glyph)
    {
        if (rune.Targeting == RuneTargeting.Self) return [Creature];

        // Runes only affect the player who inscribed them: the ally side is always just the caster, whatever the
        // target mode, so other players' Incantations never reach you (share runes with Choral Evocation instead).
        // The Friendship rune is the explicit exception: while it is active, supportive runes reach every player.
        var hitsEnemies = (rune.Targeting == RuneTargeting.Enemy) != Mirrored;
        if (!hitsEnemies)
        {
            if (rune.Targeting == RuneTargeting.Ally && Friendship != FriendshipScope.None)
                return CombatState.PlayerCreatures.Where(c => c.IsAlive).ToList();
            return [Creature];
        }

        var pool = CombatState.HittableEnemies.ToList();
        if (pool.Count == 0) return [];

        return TargetMode switch
        {
            TargetMode.Scatter => PickRandom(ScatterPool(pool)),
            TargetMode.Nova => pool,
            TargetMode.Execution => [pool.MinBy(c => c.CurrentHp)!],
            _ when glyph.Anchor != null && pool.Contains(glyph.Anchor) => [glyph.Anchor],
            _ => PickRandom(pool)
        };
    }

    /// <summary>Mirror flips sides; any later targeting rune replaces it, which is how players counter a curse Mirror.</summary>
    public void ApplyTarget(TargetMode mode)
    {
        Mirrored = mode == TargetMode.Mirror;
        TargetMode = Mirrored ? TargetMode.Anchor : mode;
    }

    public void ApplyFriendship(FriendshipScope scope) => Friendship = scope;

    /// <summary>Called after a payload glyph finishes; a next-rune-only Friendship is consumed by it.</summary>
    public void ConsumeFriendship()
    {
        if (Friendship == FriendshipScope.NextGlyph) Friendship = FriendshipScope.None;
    }

    /// <summary>Chaos Falls: Scatter picks from everyone, players included.</summary>
    private List<Creature> ScatterPool(List<Creature> enemies) =>
        Listeners.Any(l => l.ScatterTargetsAnyone)
            ? [..enemies, ..CombatState.PlayerCreatures.Where(c => c.IsAlive)]
            : enemies;

    private IReadOnlyList<Creature> PickRandom(List<Creature> pool)
    {
        if (pool.Count == 0) return [];
        return [Owner.RunState.Rng.CombatTargets.NextItem(pool)!];
    }
}
