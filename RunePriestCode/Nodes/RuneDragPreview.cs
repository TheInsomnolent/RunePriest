using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// Tracks the card the local player is dragging or aiming (<see cref="NCardPlay"/>) and the creature it is aimed at, so
/// <see cref="NRuneBuffer"/> can preview what playing it would do to the Incantation. Fed by <c>RuneDragPreviewPatches</c>.
/// </summary>
public static class RuneDragPreview
{
    private static NCardPlay? _play;
    private static Creature? _target;

    /// <summary>Bumped whenever the dragged card or its target changes (or the drag ends).</summary>
    public static int Version { get; private set; }

    public static void Begin(NCardPlay play)
    {
        _play = play;
        _target = null;
        Version++;
    }

    public static void SetTarget(NCard card, Creature? target)
    {
        if (CardNode != card || target == _target) return;
        _target = target;
        Version++;
    }

    /// <summary>Notices the drag ending: the card play frees itself whether the card was played or put back.</summary>
    public static void Poll()
    {
        if (_play == null || IsActive) return;
        _play = null;
        _target = null;
        Version++;
    }

    private static bool IsActive => _play != null && GodotObject.IsInstanceValid(_play) && !_play.IsQueuedForDeletion();

    private static NCard? CardNode => IsActive ? _play!.Holder?.CardNode : null;

    /// <summary>What the dragged card would do to <paramref name="creature"/>'s Incantation; null if nothing changes.</summary>
    public static IncantationDraft? DraftFor(Creature creature)
    {
        if (CardNode?.Model is not RunePriestCard { Owner: { } owner } card || owner.Creature != creature || !card.CanPlay())
            return null;

        var draft = IncantationDraft.For(owner);
        try
        {
            card.PreviewIncantation(draft, Anchor(card));
        }
        catch (Exception e)
        {
            // A preview must never break dragging a card.
            MainFile.Logger.Error($"[Rune] Preview of {card.Id.Entry} failed: {e}");
            return null;
        }
        return draft.HasChanges ? draft : null;
    }

    /// <summary>The aimed-at creature; with a single enemy, single-target cards can only go there.</summary>
    private static Creature? Anchor(CardModel card) =>
        _target ?? (card.TargetType == TargetType.AnyEnemy && card.CombatState?.HittableEnemies is { Count: 1 } enemies
            ? enemies[0]
            : null);
}
