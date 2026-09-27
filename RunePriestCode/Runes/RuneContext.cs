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

/// <summary>Mutable state for a single Speak (or a side-effect-free preview when <see cref="Preview"/> is set).</summary>
public sealed class RuneContext(PlayerChoiceContext choiceContext, Player owner, RuneBuffer buffer, SpeakTiming timing, RunePreview? preview = null)
{
    private readonly HashSet<Glyph> _spoken = [];
    private int _enemyChainIndex;
    private int _allyChainIndex;

    public PlayerChoiceContext ChoiceContext { get; } = choiceContext;
    public Player Owner { get; } = owner;
    public RuneBuffer Buffer { get; } = buffer;
    public SpeakTiming Timing { get; } = timing;
    public RunePreview? Preview { get; } = preview;
    public bool IsPreview => Preview != null;
    public TargetMode TargetMode { get; private set; } = TargetMode.Anchor;
    public bool Mirrored { get; private set; }

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

        var hitsEnemies = (rune.Targeting == RuneTargeting.Enemy) != Mirrored;
        var pool = hitsEnemies ? CombatState.HittableEnemies.ToList() : Allies();
        if (pool.Count == 0) return [];

        return TargetMode switch
        {
            TargetMode.Seek => PickRandom(pool),
            TargetMode.Nova => pool,
            TargetMode.Chain => [pool[hitsEnemies ? _enemyChainIndex++ % pool.Count : _allyChainIndex++ % pool.Count]],
            TargetMode.Cull => [pool.MinBy(c => c.CurrentHp)!],
            _ when glyph.Anchor != null && pool.Contains(glyph.Anchor) => [glyph.Anchor],
            _ => hitsEnemies ? PickRandom(pool) : [Creature]
        };
    }

    /// <summary>Mirror flips sides; any later targeting rune replaces it, which is how players counter a curse Mirror.</summary>
    public void ApplyTarget(TargetMode mode)
    {
        Mirrored = mode == TargetMode.Mirror;
        TargetMode = Mirrored ? TargetMode.Anchor : mode;
    }

    private List<Creature> Allies()
    {
        var allies = CombatState.GetTeammatesOf(Creature).Where(c => c.IsAlive).ToList();
        if (!allies.Contains(Creature)) allies.Insert(0, Creature);
        return allies;
    }

    private IReadOnlyList<Creature> PickRandom(List<Creature> pool)
    {
        if (pool.Count == 0) return [];
        return [Owner.RunState.Rng.CombatTargets.NextItem(pool)!];
    }
}
